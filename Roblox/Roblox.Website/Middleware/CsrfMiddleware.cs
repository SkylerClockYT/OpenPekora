using System.Text.Json;
using System.Text.Json.Serialization;
using JWT;
using JWT.Algorithms;
using JWT.Exceptions;
using JWT.Serializers;
using Microsoft.AspNetCore.Http.Extensions;
using Roblox.Exceptions;
using Roblox.Models.Sessions;
using Roblox.Website.Controllers;
using Roblox.Website.Lib;

namespace Roblox.Website.Middleware;

public class CsrfJwtEntry
{
    public string csrf { get; set; }
    public DateTime createdAt { get; set; }
}

public class CsrfMiddleware : ControllerServicesExtended
{
    private RequestDelegate _next;

    public const string CookieName = "rbxcsrf4";

    // JWT Config
    private static readonly IJwtAlgorithm Algorithm = new HMACSHA512Algorithm();
    private static readonly IJsonSerializer Serializer = new JsonNetSerializer();
    private static readonly IBase64UrlEncoder UrlEncoder = new JwtBase64UrlEncoder();
    private static readonly IDateTimeProvider DateTimeProvider = new UtcDateTimeProvider();
    private static readonly IJwtValidator Validator = new JwtValidator(Serializer, DateTimeProvider);

    private static readonly IJwtEncoder Encoder = new JwtEncoder(Algorithm, Serializer, UrlEncoder);
    private static readonly IJwtDecoder Decoder = new JwtDecoder(Serializer, Validator, UrlEncoder, Algorithm);

    private static string cookieJwtKey { get; set; }

    public static void Configure(string newJwtKey)
    {
        if (cookieJwtKey != null) throw new Exception("Already configured");
        cookieJwtKey = newJwtKey;
    }


    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public static string CreateJwt<T>(T obj)
    {
        var token = Encoder.Encode(obj, cookieJwtKey);
        if (token == null) throw new NullReferenceException();
        return token;
    }

    public static T DecodeJwt<T>(string token)
    {
        var json = Decoder.Decode(token, cookieJwtKey, verify: true);
        if (json == null) throw new NullReferenceException();
        var result = JsonSerializer.Deserialize<T>(json);
        if (result == null) throw new NullReferenceException();
        return result;
    }

    public CsrfJwtEntry? TryGetCookie(HttpContext ctx)
    {
        if (ctx.Request.Cookies.ContainsKey(CookieName))
        {
            var cookie = ctx.Request.Cookies[CookieName];
            var decodedResult = DecodeJwt<CsrfJwtEntry>(cookie!);
            if (!string.IsNullOrEmpty(decodedResult.csrf) &&
                decodedResult.createdAt.Add(TimeSpan.FromMinutes(5)) >= DateTime.UtcNow)
            {
                return decodedResult;
            }
        }

        return null;
    }

    public async Task OnTokenFail(HttpContext ctx)
    {
        var csrfBits = new Byte[8];
        new Random().NextBytes(csrfBits);
        var newToken = new CsrfJwtEntry()
        {
            csrf = Convert.ToBase64String(csrfBits),
            createdAt = DateTime.UtcNow,
        };
        var tokenSerialized = CreateJwt(newToken);
        ctx.Response.Cookies.Append(CookieName, tokenSerialized, new()
        {
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true,
            HttpOnly = true,
            MaxAge = TimeSpan.FromMinutes(4),
        });
        await SendTokenFailMessage(ctx, newToken.csrf);
    }

    public async Task SendTokenFailMessage(HttpContext ctx, string csrf)
    {
        ctx.Response.StatusCode = 403;
        ctx.Response.Headers.Append("x-csrf-token", csrf);
        var url = ctx.Request.GetEncodedUrl();
        await ctx.Response.WriteAsJsonAsync(new
        {
            errors = new List<dynamic>()
            {
                new
                {
                    code = 0,
                    message = "Token Validation Failed",
                }
            }
        });
    }


    public static List<string> bypassUrls = new()
    {
        // gs
        "/gs/activity",
        "/gs/ping",
        "/gs/delete",
        "/gs/shutdown",
        "/gs/players/report",
        "/gs/a",
        "/game/validateticket.ashx",
        "/api/moderation/filtertext",
        "/moderation/filtertext",
        "/moderation/v2/filtertext",
        "/develop/upload-version",
        // universes
        "/universes/",
        // uses built-in RequestVerificationToken
        "/auth",
        "/auth/signup",
        "/auth/discord",
        "/auth/application",
        "/auth/application-check",
        "/internal/year",
        "/internal/clothingstealer",
        "/internal/report-abuse",
        "/internal/age",
        "/internal/membership",
        "/internal/apply",
        "/internal/place-update",
        "/internal/migrate-to-application",
        "/internal/contest/first-contest",
        "/internal/tixexchange",
        "/internal/robuxexchange",
        "/internal/referral",
        "/auth/account-deletion",
        "/auth/2fa",
        "/auth/accountlogin",
        "/auth/password-reset",
        "/auth/ticket",
        "/auth/captcha",
        "/internal/invite",
        "/internal/create-place",
        "/internal/promocodes",
        "/auth/notapproved",
        // hubs
        "/chat",
        "/chat/negotiate",
        "/v1/router",
        "/version",
        "/v1/CreateOrUpdate",
        "/game/validate-machine",
        //economy
        "v1/purchases/products",
    };

    public async Task InvokeAsync(HttpContext ctx)
    {
        var csrfTimer = new MiddlewareTimer(ctx, "c");
        #if DEBUG
        var swag = ctx.Request.Headers.Referer;
        if (swag.ToString().EndsWith("/swagger/index.html"))
        {
            await _next(ctx);
            return;
        }
        #endif
        var pathLower = ctx.Request.Path.ToString().ToLower();
        var fullUrl = ctx.Request.GetEncodedUrl().ToLower();
        if (pathLower.EndsWith("/"))
        {
            pathLower = pathLower.Substring(0, pathLower.Length - 1);
        }
        try
        {
            bool isBypassed = bypassUrls.Any(bypassPath => pathLower.Contains(bypassPath, StringComparison.OrdinalIgnoreCase));

            if (ctx.Request.Method != "GET" && ctx.Request.Method != "OPTIONS" && ctx.Request.Method != "HEAD" && !pathLower.Contains("v1/router") && !pathLower.Contains("multiget-friend-requests") && !pathLower.Contains("filter-friends") && !pathLower.Contains("abusereport") && !isBypassed && !pathLower.Contains("enablecloudedit")  && !pathLower.Contains("v1/purchases/products") && !pathLower.Contains("teamcreate"))
            {
                var token = TryGetCookie(ctx);
                var provided = ctx.Request.Headers["x-csrf-token"].ToList();
                var userAgent = ctx.Request.Headers["User-Agent"].ToString();
                var stupidHeader = ctx.Request.Headers["stupid"].ToString();
                if (fullUrl.Contains("messagerouter"))
                {
                    Console.WriteLine($"MEOW MEOW MEOW MEOW MEOW: {fullUrl}");
                }
                if (userAgent.ToLower().Contains("roblox") || userAgent == Configuration.UserAgentBypassSecret)
                {
                    await _next(ctx);
                    return;
                }
                if (token == null)
                {
#if DEBUG
                    Console.WriteLine("[info] CSRF fail for {0} (P={1})",ctx.Request.GetEncodedUrl(), ctx.Request.Path.ToString());
#endif
                    await OnTokenFail(ctx);
                    return;
                }
                else if (provided.Count < 1 || provided[0] != token.csrf)
                {
#if DEBUG
                    Console.WriteLine("[info] CSRF fail for {0} (P={1})",ctx.Request.GetEncodedUrl(), ctx.Request.Path.ToString());
#endif
                    await SendTokenFailMessage(ctx, token.csrf);
                    return;
                }
            }
        }
        catch (System.Exception e) when (e is InvalidTokenPartsException or NullReferenceException or SignatureVerificationException)
        {
            await OnTokenFail(ctx);
            return;
        }

        csrfTimer.Stop();
        await _next(ctx);
    }
}

public static class CsrfMiddlewareExtension
{
    public static IApplicationBuilder UseRobloxCsrfMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CsrfMiddleware>();
    }
}