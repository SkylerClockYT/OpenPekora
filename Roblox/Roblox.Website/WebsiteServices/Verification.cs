using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Dto.Users;
using Roblox.Exceptions;
using Roblox.Libraries.EasyJwt;
using Roblox.Libraries.RobloxApi;
using Roblox.Libraries.TwitterApi;
using Roblox.Logging;
using Roblox.Services;
using Dapper;
using Roblox.Website.Pages.Auth;

using ServiceProvider = Roblox.Services.ServiceProvider;

namespace Roblox.Website.WebsiteServices;

public class VerificationResult
{
    /// <summary>
    /// Raw app social data, such as the name/id
    /// </summary>
    public AppSocialMedia socialData { get; set; }
    /// <summary>
    /// Whether the profile was automatically verified
    /// </summary>
    public bool isVerified => verifiedId != null && verifiedUrl != null;
    public string? verifiedId { get; set; }
    public string? verifiedUrl { get; set; }
    /// <summary>
    /// The users social media URL. This is equal to <see cref="verifiedUrl"/> if the account was verified.
    /// </summary>
    public string normalizedUrl { get; set; }
    /// <summary>
    /// Whether the profile seems to belong to a user under the age of 13
    /// </summary>
    public bool isUnderageUser { get; set; }
}

// TODO: remove or recycle this class as it's no longer used since we now parse the username into a userid 
public class InvalidSocialMediaUrlException : Exception {}
public class AccountTooNewException : Exception {}

public class UnableToFindVerificationPhraseException : Exception
{
    public UnableToFindVerificationPhraseException(string message) : base(message) {}
}

public class ApplicationWebsiteService : WebsiteService
{

    private const string VerificationPhraseCookieName = "es-verification-phrase";
    private static EasyJwt jwt { get; } = new EasyJwt();
    private string VerificationSecret = Configuration.VerificationSecret;

    /// <summary>
    /// Verify that the provided verificationPhrase exists on the socialUrls profile
    /// </summary>
    /// <param name="socialUrl"></param>
    /// <param name="verificationPhrase"></param>
    /// <returns></returns>
    /// <exception cref="InvalidSocialMediaUrlException">Social media URL is invalid (e.g. cannot be verified)</exception>
    /// <exception cref="AccountTooNewException">Social media account was created too recently</exception>
    /// <exception cref="UnableToFindVerificationPhraseException">Verification phrase does not exist on the profile</exception>
    /// <exception cref="NotImplementedException">The URL was parsed correctly, but support has not yet been added</exception>
    public async Task<VerificationResult> AttemptVerifyUser(string socialUrl, string verificationPhrase)
    {
        // WEB-25 - people apparently can't read...
        string? verifiedUrl = null;
        string? verifiedId = null;

        // Users will submit apps with weird text like:
        // "roblox: https://www.roblox.com/users/1561515/profile reddit: https://www.reddit.com/users/spez"
        // so we can't use a basic URI parser, we have to make our own
        var socialData = AppSocialMedia.ParseFirstUrl(socialUrl);
        if (socialData != null && socialData.IsRedirectProfile())
        {
            // Can be null if redirect is bad, so do this before null check
            socialData = await socialData.GetFullProfileAsync();
            Writer.Info(LogGroup.ApplicationSocial, "redirect from {0} to {1}", socialUrl, socialData?.url);
        }

        if (socialData == null)
            throw new InvalidSocialMediaUrlException();
            
        // UPDATE! We no longer check for underage users. Since this is no longer possible since the deprecated web.roblox.com
        // var isUnderageUser = socialUrl.Contains("web.roblox.com");
        switch (socialData.site)
        {
            case SocialMediaSite.RobloxUserId:
                var roblox = new RobloxApi();
                var userId = long.Parse(socialData.identifier);
                var userDesc = await roblox.GetUserInfo(userId);
                if (userDesc.created == null || userDesc.description == null)
                {
                    throw new InvalidSocialMediaUrlException();
                }
#if !DEBUG
                var created = DateTime.Parse(userDesc.created);
                if (created >= DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)))
                {
                    throw new AccountTooNewException();
                }

                if (!AppSocialMedia.IsVerificationPhraseInString(verificationPhrase, userDesc.description))
                {
                    throw new UnableToFindVerificationPhraseException("Could not find verification phrase in your Roblox about me section. If you recently updated your \"about\" section, you may have to wait a few minutes and try again.");
                }
#endif
                verifiedUrl = socialData.url;
                verifiedId = socialData.site + ":" + socialData.identifier;
                break;
            default:
                throw new NotImplementedException();
        }

        
        // web.roblox.com isn't used anymore, so we can just return false here.
        return new VerificationResult()
        {
            verifiedId = verifiedId,
            verifiedUrl = verifiedUrl,
            normalizedUrl = socialUrl,
            socialData = socialData,
            isUnderageUser = false,
        };
    }

    public void DeleteVerificationCookie()
    {
        httpContext.Response.Cookies.Delete(VerificationPhraseCookieName);
    }

    /// <summary>
    /// Get phrase from cookie or generate a new phrase. Returns the phrase.
    /// </summary>
    /// <param name="userIp"></param>
    /// <param name="ctx">Generation context</param>
    /// <param name="forceGenerateNew"></param>
    /// <returns></returns>
    /// <exception cref="TooManyRequestsException"></exception>
    public async Task<string> ApplyVerificationPhrase(string userIp, ApplicationService.GenerationContext ctx, bool forceGenerateNew = false)
    {
        var verificationPhrase = (string?)null;

        if (httpContext.Request.Cookies.TryGetValue(VerificationPhraseCookieName, out var verificationPhraseCookie) && verificationPhraseCookie != null)
        {
            try
            {
                var result = jwt.DecodeJwt<VerificationPhraseCookie>(verificationPhraseCookie, VerificationSecret);
                var isExpired = result.createdAt.AddHours(1) < DateTime.UtcNow;
                Writer.Info(LogGroup.AbuseDetection, "expired? {0}", isExpired);
                if (!isExpired)
                {
                    verificationPhrase = result.phrase;
                }
            }
            catch (Exception e)
            {
                // A lot of stuff can go wrong here.
                Writer.Info(LogGroup.AbuseDetection, "Failed to decode verification phrase cookie", e);
            }
        }

        var cacheKey = "VerificationPhrase:" + ctx + ":v1:" + userIp;

        if (verificationPhrase == null || forceGenerateNew)
        {
            using var cooldown = ServiceProvider.GetOrCreate<CooldownService>();
            var rateLimitAttempt = await cooldown.TryIncrementBucketCooldown("VerifyPhraseGen:v1:" + userIp, 10, TimeSpan.FromHours(1));
            // do we already have one?
            var existing = await Roblox.Services.Cache.distributed.StringGetAsync(cacheKey);
            if (existing != null && !forceGenerateNew)
            {
                verificationPhrase = existing.ToString();
            }
            else
            {
                if (!rateLimitAttempt)
                {
                    throw new TooManyRequestsException();
                }
                // We have to generate a new phrase.
                using (var app = ServiceProvider.GetOrCreate<ApplicationService>())
                {
                    verificationPhrase = app.GenerateVerificationPhrase(ctx);
                }

                var cookie = jwt.CreateJwt(new VerificationPhraseCookie()
                {
                    phrase = verificationPhrase,
                    createdAt = DateTime.UtcNow,
                }, VerificationSecret);

                httpContext.Response.Cookies.Append(VerificationPhraseCookieName, cookie, new CookieOptions
                {
                    IsEssential = true,
                    Path = "/",
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                });
                // save it to redis
                await Roblox.Services.Cache.distributed.StringSetAsync(cacheKey, verificationPhrase, TimeSpan.FromHours(1));
            }
        }

        return verificationPhrase;
    }

    public ApplicationWebsiteService(HttpContext ctx) : base(ctx)
    {
    }
}