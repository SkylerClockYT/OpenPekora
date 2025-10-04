using MVC = Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Roblox.Exceptions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class FeatureFlagsRoblox: ControllerBase
    {
        [HttpPostBypass("Setting/Get/{type}")]
        [HttpPostBypass("Setting/QuietGet/{type}")]
        [HttpGetBypass("Setting/Get/{type}")]
        [HttpGetBypass("Setting/QuietGet/{type}")]
        public MVC.ActionResult<dynamic> GetApplicationSettingsLegacy(string type, string apiKey)
        {
            return Content(GetFeatureFlags(type, apiKey), "application/json");
        }

        [HttpPostBypass("v2/settings/application")]
        [HttpGetBypass("v2/settings/application")]
        [HttpPostBypass("v1/settings/application")]
        [HttpGetBypass("v1/settings/application")]
        public MVC.ActionResult<dynamic> GetApplicationSettingsModern(string applicationName)
        {
            return Content(GetFeatureFlags(applicationName), "application/json");
        }

        // For modern clients
        private static readonly HashSet<string> applicationNames = new HashSet<string>
        {
            "RCCService2019",
            "PCDesktopClient2019",
            "RCCService2020",
            "PCStudioApp",
            "PCStudio221",
            "PCStudio223",
            "RCCService2021",
            "RCCServiceGDASTGWG72713", // 2021 Too
            "PCDesktopClient",
            "PCDesktopClient2021",
            "PCDesktopCli223",
            "AndroidApp",
            "iOSApp"
        };
        // For legacy clients
        private string GetTypeForApiKey(string type, string apiKey)
        {
            switch (apiKey)
            {
                case "9CE2063F-BB45-449B-89D4-65CD2ED806CD":  //2017L RCC
                    type = "RCCServiceUJ38BA31M8F47VA76XZ1RYONSSTILA3F";
                    break;
                case "D6925E56-BFB9-4908-AAA2-A5B1EC4B2D79":
                case "08BF6621-8100-4484-B14C-87497E372160": //2017L Studio + Client
                    if(type == "StudioAppSettings")
                        break;
                    type = "ClientAppSettings2017";
                    break;
                case "D6925E56-BFB9-4908-AAA2-A5B1EC4B2D7A":  //2018L RCC
                    type = "RCCService2018";
                    break;
                case "76E5A40C-3AE1-4028-9F10-7C62520BD94F":
                case "19C0B314-AC23-4CD4-8A37-02C4140F7240":
                    type = "ClientAppSettings2018";
                    break;
                default:
                    throw new BadRequestException(0, $"Invalid API key: {apiKey}");
            }
            return type;
        }
        private string GetFeatureFlags(string type, string? apiKey = null)
        {
            /*
                The legacy clients use an API key and a type to get the feature flags
                Modern clients only use the type.
                Here we do a few sanity checks to make sure the request is valid.
            */
            if (apiKey != null)
                type = GetTypeForApiKey(type, apiKey);
            else if (!applicationNames.Contains(type))
                throw new BadRequestException(1, $"Invalid application name: {type}");

            if (type == "PCStudio221")
                type = "PCDesktopClient2021";
            // temp
            if (type == "RCCServiceGDASTGWG72713")
                type = "RCCService2021";
            string featureFlags = Path.Join(Configuration.JsonDataDirectory, $"{type}.json");
            
            // Also should never happen, but just in case
            if (!System.IO.File.Exists(featureFlags))
                throw new BadRequestException(0, $"Feature flags not found for {type}");

            return System.IO.File.ReadAllText(featureFlags);
        }
    }
}