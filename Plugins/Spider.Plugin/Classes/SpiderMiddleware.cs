﻿/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *  .Net Core Plugin Manager is distributed under the GNU General Public License version 3 and  
 *  is also available under alternative licenses negotiated directly with Simon Carter.  
 *  If you obtained Service Manager under the GPL, then the GPL applies to all loadable 
 *  Service Manager modules used on your system as well. The GPL (version 3) is 
 *  available at https://opensource.org/licenses/GPL-3.0
 *
 *  This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *  See the GNU General Public License for more details.
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2018 - 2020 Simon Carter.  All Rights Reserved.
 *
 *  Product:  Spider.Plugin
 *  
 *  File: SpiderMiddleware.cs
 *
 *  Purpose:  
 *
 *  Date        Name                Reason
 *  29/09/2018  Simon Carter        Initially Created
 *  13/10/2018  Simon Carter
 *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using PluginManager;
using PluginManager.Abstractions;

using Shared.Classes;

using SharedPluginFeatures;

#pragma warning disable CS1591

namespace Spider.Plugin
{
    /// <summary>
    /// Spider middleware, serves robots.txt on request and denies access to route for spider connections.
    /// </summary>
    public sealed class SpiderMiddleware : BaseMiddleware
    {
        #region Private Members

        private byte[] _spiderData;
        private readonly List<DeniedRoute> _deniedSpiderRoutes;
        private readonly bool _userSessionManagerLoaded;
        private readonly RequestDelegate _next;
        private readonly bool _processStaticFiles;
        private readonly string _staticFileExtensions = Constants.StaticFileExtensions;
        private readonly INotificationService _notificationService;
        private readonly ILogger _logger;

        internal static Timings _timings = new Timings();

        #endregion Private Members

        #region Constructors

        public SpiderMiddleware(RequestDelegate next, IActionDescriptorCollectionProvider routeProvider,
            IRouteDataService routeDataService, IPluginHelperService pluginHelperService,
            IPluginTypesService pluginTypesService, ISettingsProvider settingsProvider,
            ILogger logger, INotificationService notificationService)
        {
            if (routeProvider == null)
                throw new ArgumentNullException(nameof(routeProvider));

            if (routeDataService == null)
                throw new ArgumentNullException(nameof(routeDataService));

            if (pluginHelperService == null)
                throw new ArgumentNullException(nameof(pluginHelperService));

            if (settingsProvider == null)
                throw new ArgumentNullException(nameof(settingsProvider));

            if (pluginTypesService == null)
                throw new ArgumentNullException(nameof(pluginTypesService));

            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            _next = next;

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userSessionManagerLoaded = pluginHelperService.PluginLoaded(Constants.PluginNameUserSession, out int _);

            _deniedSpiderRoutes = new List<DeniedRoute>();
            LoadSpiderData(routeProvider, routeDataService, pluginTypesService);

            SpiderSettings settings = settingsProvider.GetSettings<SpiderSettings>("Spider.Plugin");

            _processStaticFiles = settings.ProcessStaticFiles;

            if (!String.IsNullOrEmpty(settings.StaticFileExtensions))
                _staticFileExtensions = settings.StaticFileExtensions;
        }

        #endregion Constructors

        #region Public Methods

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "it's ok here, nothing to see, move along")]
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            string fileExtension = RouteFileExtension(context);

            if (!_processStaticFiles && !String.IsNullOrEmpty(fileExtension) &&
                _staticFileExtensions.Contains($"{fileExtension};"))
            {
                await _next(context);
                return;
            }

            using (StopWatchTimer stopwatchTimer = StopWatchTimer.Initialise(_timings))
            {
                string route = RouteLowered(context);

                if (route.EndsWith("/robots.txt"))
                {
                    context.Response.StatusCode = 200;

                    // prepend sitemaps if there are any
                    object notificationResult = new object();

                    if (_notificationService.RaiseEvent(Constants.NotificationSitemapNames, context, null, ref notificationResult))
                    {
                        string[] sitemaps = ((System.Collections.IEnumerable)notificationResult)
                          .Cast<object>()
                          .Select(x => x.ToString())
                          .ToArray();

                        if (sitemaps != null)
                        {
                            StringBuilder stringBuilder = new StringBuilder();
                            string url = GetHost(context);

                            for (int i = 0; i < sitemaps.Length; i++)
                            {
                                stringBuilder.Append($"Sitemap: {url}{sitemaps[i].Substring(1)}\r\n\r\n");
                            }

                            await context.Response.WriteAsync(stringBuilder.ToString());
                        }
                    }

                    context.Response.Body.Write(_spiderData, 0, _spiderData.Length);
                }
                else
                {
                    if (_userSessionManagerLoaded)
                    {
                        if (context.Items.ContainsKey(Constants.UserSession))
                        {
                            try
                            {
                                UserSession userSession = (UserSession)context.Items[Constants.UserSession];

                                foreach (DeniedRoute deniedRoute in _deniedSpiderRoutes)
                                {
                                    if (userSession.IsBot &&
                                        deniedRoute.Route.StartsWith(route) &&
                                        (
                                            deniedRoute.UserAgent == "*" ||
#if NET_CORE
                                            userSession.UserAgent.Contains(deniedRoute.UserAgent, StringComparison.CurrentCultureIgnoreCase)
#else 
                                            userSession.UserAgent.ToLower().Contains(deniedRoute.UserAgent.ToLower())
#endif
                                        ))
                                    {
                                        context.Response.StatusCode = 403;
                                        return;
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                _logger.AddToLog(LogLevel.Error, nameof(SpiderMiddleware), err, MethodBase.GetCurrentMethod().Name);
                            }
                        }
                    }
                }
            }

            await _next(context);
        }

        #endregion Public Methods

        #region Private Methods

        private void LoadSpiderData(IActionDescriptorCollectionProvider routeProvider,
            IRouteDataService routeDataService, IPluginTypesService pluginTypesService)
        {
            string spiderTextFile = String.Empty;
            List<Type> spiderAttributes = pluginTypesService.GetPluginTypesWithAttribute<DenySpiderAttribute>();

            if (spiderAttributes.Count == 0)
            {
                spiderTextFile = "# Allow all from Spider.Plugin\r\n\r\nUser-agent: *";
            }
            else
            {
                // Cycle through all classes and methods which have the spider attribute
                foreach (Type type in spiderAttributes)
                {
                    // is it a class attribute
                    DenySpiderAttribute attribute = (DenySpiderAttribute)type.GetCustomAttributes(true)
                        .Where(a => a.GetType() == typeof(DenySpiderAttribute)).FirstOrDefault();

                    if (attribute != null)
                    {
                        string route = routeDataService.GetRouteFromClass(type, routeProvider);

                        if (String.IsNullOrEmpty(route))
                            continue;

                        if (!String.IsNullOrEmpty(attribute.Comment))
                            spiderTextFile += $"# {attribute.Comment}\r\n\r\n";

                        spiderTextFile += $"User-agent: {attribute.UserAgent}\r\n";
                        spiderTextFile += $"Disallow: /{route}/\r\n\r\n";

                        _deniedSpiderRoutes.Add(new DeniedRoute($"/{route.ToLower()}/", attribute.UserAgent));
                    }

                    // look for specific method disallows

                    foreach (MethodInfo method in type.GetMethods())
                    {
                        attribute = (DenySpiderAttribute)method.GetCustomAttributes(true)
                            .Where(a => a.GetType() == typeof(DenySpiderAttribute)).FirstOrDefault();

                        if (attribute != null)
                        {
                            string route = routeDataService.GetRouteFromMethod(method, routeProvider);

                            if (String.IsNullOrEmpty(route))
                                continue;

                            if (!String.IsNullOrEmpty(attribute.Comment))
                                spiderTextFile += $"# {attribute.Comment}\r\n\r\n";

                            spiderTextFile += $"User-agent: {attribute.UserAgent}\r\n";
                            spiderTextFile += $"Disallow: {route}\r\n\r\n";

                            _deniedSpiderRoutes.Add(new DeniedRoute($"{route.ToLower()}", attribute.UserAgent));
                        }
                    }
                }
            }

            _spiderData = Encoding.UTF8.GetBytes(spiderTextFile);
        }

        #endregion Private Methods
    }
}

#pragma warning restore CS1591