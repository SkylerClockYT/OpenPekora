using System.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Roblox.Website.Lib;

namespace Roblox.Website.Middleware;

public class RobloxLoggingMiddleware
{
    private RequestDelegate _next;
    public RobloxLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        string encodedUrl = ctx.Request.GetEncodedUrl();
        var watch = new Stopwatch();
        watch.Start();
        await _next(ctx);
        watch.Stop();

        var str = $"[{ctx.Request.Method.ToUpper()}] {encodedUrl} - {watch.ElapsedMilliseconds}ms";

        //if (
        //    encodedUrl.Contains(".png") ||
        //    encodedUrl.Contains("apisite") ||
        //    encodedUrl.Contains("Avatar.ashx") ||
        //    encodedUrl.Contains("asset") ||
        //    encodedUrl.Contains("CreateOrUpdate") ||
        //    encodedUrl.Contains("v2.0/Refresh")
        //    )
        //{
        //    return;
        //}

        if (watch.ElapsedMilliseconds >= 3000)
        {
            str = $"[SLOW] {str}";
            Console.WriteLine(str);
        }
        return;
    }
}

public static class RobloxLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRobloxLoggingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RobloxLoggingMiddleware>();
    }
}