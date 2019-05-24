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
 *  The Original Code was created by unknown and modified by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2012 - 2018 Simon Carter.  All Rights Reserved.
 *
 *  Product:  SharedPluginFeatues
 *  
 *  File: Constants.cs
 *
 *  Purpose:  Shared constant values.
 *
 *  Date        Name                Reason
 *  09/12/2018  Simon Carter        Initially Created
 *  12/05/2019  Simon Carter        Add seo data constants
 *  
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */


namespace SharedPluginFeatures
{
    /// <summary>
    /// Constant values shared between all plugin modules
    /// </summary>
    public sealed class Constants
    {
        public const int MinimumPasswordLength = 8;
        public const int MaximumPasswordLength = 40;


        public const string UserSession = "UserSession";
        public const string UserCulture = "UserCulture";
        public const string Breadcrumbs = "Breadcrumbs";
        public const string ShoppingCart = "ShoppingCart";
        public const string BasketSummary = "BasketSummary";
        public const string DefaultTaxRate = "DefaultTaxRate";
        public const string SeoTitle = "SeoTitle";
        public const string SeoMetaDescription = "SeoMetaDescription";
        public const string SeoMetaKeywords = "SeoTags";
        public const string SeoMetaAuthor = "SeoAuthor";

        public const string DefaultSessionCookie = "user_session";

        public const string UserSessionConfiguration = "UserSessionConfiguration";

        public const string StaticFileExtensions = ".less;.ico;.css;.js;.svg;.jpg;.jpeg;.gif;.png;.eot;.map;";

        public const string PageReferer = "Referer";


        public const string ForwardSlash = "/";


        public const char ForwardSlashChar = '/';
        public const char Dash = '-';


        public const string PluginSettingBreadcrumb = "Breadcrumb.Plugin";


        public const string PluginNameUserSession = "UserSessionMiddleware.Plugin.dll";
        public const string PluginNameLocalization = "Localization.Plugin.dll";
        public const string PluginNameShoppingCart = "ShoppingCartPlugin.dll";


        public const string UserSessionServiceNotFound = "UserSessionService not found";

        public const string BreadcrumbRoutEqualsParentRoute = "Parent route matches route; Route: {0}; Parent Route {1}";
        public const string TooManyBreadcrumbs = "Breadcrumb recursion, check parent route values";

        public const string InvalidListener = "Listener must provide at least one event";
        public const string InvalidEventName = "Invalid event name in listener";
        public const string EventNameNotRegistered = "Event name has not been registered";

        public const string CurrencyCodeDefault = "GBP";

        public const string ThreadNotificationService = "Notification Service";

        /// <summary>
        /// Name of the documentation file cache
        /// </summary>
        public const string DocumentationFileCache = "DocumentationFileCache";

        /// <summary>
        /// Name of the documentation list cache
        /// </summary>
        public const string DocumentationListCache = "DocumentationListCache";
    }
}
