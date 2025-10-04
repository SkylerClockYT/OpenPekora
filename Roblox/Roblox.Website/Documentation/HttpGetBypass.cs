// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;
using Roblox.Website.Middleware;

namespace Roblox.Website.Controllers
{
    public class BypassConfiguration
    {
        public static void AddToBypass(string template)
        {
            if (!FrontendProxyMiddleware.BypassUrls.Contains(template))
            {
                var toAdd = template;
                if (!toAdd.StartsWith("/"))
                {
                    toAdd = "/" + toAdd;
                }

                if (toAdd.IndexOf("{") != -1)
                {
                    toAdd = toAdd.Substring(0, toAdd.IndexOf("{"));
                }

                toAdd = toAdd.ToLower();
                Console.WriteLine("[info] add to bypass {0}", toAdd);
                FrontendProxyMiddleware.BypassUrls.Add(toAdd);
                ApplicationGuardMiddleware.allowedUrls.Add(toAdd);
                CsrfMiddleware.bypassUrls.Add(toAdd);
            }
        }
    }
    /// <summary>
    /// Identifies an action that supports the HTTP GET method.
    /// </summary>
    public class HttpGetBypassAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "GET" };

        private static readonly List<string> ViewUrls = new List<string>()
        {
            "game/visit.ashx",
            "IDE/welcome",
            "/ide/publish/editplace",
            "IDE/Upload.aspx",
            "IDE/login",
            "IDE/ClientToolbox.aspx"
        };

        static HttpGetBypassAttribute()
        {
            foreach (var url in ViewUrls)
            {
                BypassConfiguration.AddToBypass(url);
            }
        }

        /// <summary>
        /// Creates a new <see cref="HttpGetBypassAttribute"/>.
        /// </summary>
        public HttpGetBypassAttribute()
            : base(_supportedMethods)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpGetBypassAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpGetBypassAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            BypassConfiguration.AddToBypass(template);
        }
    }
    
    /// <summary>
    /// Identifies an action that supports the HTTP DELETE method.
    /// </summary>
    public class HttpDeleteBypassAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new [] { "DELETE" };

        /// <summary>
        /// Creates a new <see cref="HttpDeleteBypassAttribute"/>.
        /// </summary>
        public HttpDeleteBypassAttribute()
            : base(_supportedMethods)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpDeleteBypassAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpDeleteBypassAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
            BypassConfiguration.AddToBypass(template);
        }
    }


    /// <summary>
    /// Identifies an action that supports the HTTP PATCH method.
    /// </summary>
    public class HttpPatchBypassAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new [] { "PATCH" };

        /// <summary>
        /// Creates a new <see cref="HttpPatchBypassAttribute"/>.
        /// </summary>
        public HttpPatchBypassAttribute()
            : base(_supportedMethods)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpPatchBypassAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpPatchBypassAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
            BypassConfiguration.AddToBypass(template);
        }
    }

    /// <summary>
    /// Identifies an action that supports the HTTP POST method.
    /// </summary>
    public class HttpPostBypassAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new [] { "POST" };

        /// <summary>
        /// Creates a new <see cref="HttpPostBypassAttribute"/>.
        /// </summary>
        public HttpPostBypassAttribute()
            : base(_supportedMethods)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpPostBypassAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpPostBypassAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
            BypassConfiguration.AddToBypass(template);
        }
    }
}