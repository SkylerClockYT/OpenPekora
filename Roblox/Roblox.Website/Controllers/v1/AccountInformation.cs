using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Roblox.Exceptions;
using Roblox.Models.Users;
using Roblox.Website.WebsiteModels;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/accountinformation/v1")]
public class AccountInformationControllerV1 : ControllerBase
{
    [HttpGet("users/{userId:long}/roblox-badges")]
    public async Task<dynamic> GetRobloxBadges(long userId)
    {
        return await services.accountInformation.GetUserBadges(userId);
    }

    [HttpGet("metadata")]
    public dynamic GetMetadata()
    {
        return new
        {
            isAllowedNotificationsEndpointDisabled = true,
            isAccountSettingsPolicyEnabled = true,
            isPhoneNumberEnabled = false,
            MaxUserDescriptionLength = 1000,
            isUserDescriptionEnabled = false,
            isUserBlockEndpointsUpdated = false
        };
    }

    [HttpGet("phone")]
    public dynamic GetPhone()
    {
        return new
        {
            countryCode = (int?)null,
            prefix = (int?)null,
            phone = (int?)null,
            isVerified = false,
            verificationCodeLength = 6
        };
    }

    [HttpGet("description")]
    public async Task<dynamic> GetUserDescription()
    {
        var info = await services.users.GetUserById(safeUserSession.userId);
        return new
        {
            description = info.description,
        };
    }

    [HttpPost("description")]
    public async Task UpdateDescription([Required, FromBody] UpdateDescriptionRequest request)
    {
        if (request.description is {Length: >= 1024})
        {
            throw new BadRequestException(0, "BadRequest");
        }

        await services.users.SetUserDescription(safeUserSession.userId, request.description);
    }

    [HttpGet("birthdate")]
    public dynamic GetBirthDate()
    {
        return new
        {
            birthMonth = 9,
            birthDay = 11,
            birthYear = 2001,
        };
    }

    [HttpPost("birthdate")]
    public void SetBirthDate()
    {
        
    }

    [HttpGet("gender")]
    public async Task<dynamic> GetGender()
    {
        var result = await services.accountInformation.GetUserGender(safeUserSession.userId);
        return new
        {
            gender = result,
        };
    }

    [HttpPost("gender")]
    public async Task SetGender([Required, FromBody] UpdateGenderRequest request)
    {
        await services.accountInformation.SetUserGender(safeUserSession.userId, request.gender);
    }
    
    [HttpGet("avatar-page-style")]
    public async Task<dynamic> GetAvatarPageStyle()
    {
        var result = await services.accountInformation.GetUserAvatarPageStyle(safeUserSession.userId);
        return new
        {
            avatarPageStyle = result,
        };
    }

    [HttpPost("avatar-page-style")]
    public async Task SetAvatarPageStyle([Required, FromBody] UpdateAvPageStyleRequest request)
    {
        await services.accountInformation.SetUserAvatarPageStyle(safeUserSession.userId, request.avatarPageStyle);
    }

    [HttpGet("promotion-channels")]
    [HttpGetBypass("/v1/promotion-channels")]
    public async Task<dynamic> GetPromotionChannels(bool alwaysReturnUrls) {
        var conn = await services.accountInformation.GetUserConnections(safeUserSession.userId);
        var discordUrl = alwaysReturnUrls && conn.discord != null && IsDigits(conn.discord)
            ? "https://discord.com/users/" + conn.discord
            : conn.discord != null ? "@" + conn.discord : null;
        return new
        {
            promotionChannelsVisibilityPrivacy = "NoOne",
            twitter = conn.twitter != null
                ? (alwaysReturnUrls ? "https://x.com/" : "@") + conn.twitter
                : null,
            discord = discordUrl,
            telegram = conn.telegram != null
                ? (alwaysReturnUrls ? "https://t.me/" : "@") + conn.telegram
                : null,
            tiktok = conn.tiktok != null
                ? (alwaysReturnUrls ? "https://tiktok.com/@" : "@") + conn.tiktok
                : null,
            youtube = conn.youtube != null
                ? (alwaysReturnUrls ? "https://youtube.com/@" : "@") + conn.youtube
                : null,
            twitch = conn.twitch != null
                ? (alwaysReturnUrls ? "https://twitch.tv/" : "@") + conn.twitch
                : null,
            github = conn.github != null
                ? (alwaysReturnUrls ? "https://github.com/" : "@") + conn.github
                : null,
            roblox = conn.roblox != null
                ? (alwaysReturnUrls ? "https://www.roblox.com/users/profile?username=" : "@") + conn.roblox
                : null,
            // these are always null!
            facebook = (string?) null,
            guilded = (string?) null,
        };
    }
    
    [HttpGet("users/{userId:long}/promotion-channels")]
    [HttpGetBypass("/v1/users/{userId:long}/promotion-channels")]
    public async Task<dynamic> GetPromotionChannelsFromUserId(long userId, bool alwaysReturnUrls = false) {
        var _ = safeUserSession.userId; // confirm user is logged in
        var conn = await services.accountInformation.GetUserConnections(userId);
        var discordUrl = alwaysReturnUrls && conn.discord != null && IsDigits(conn.discord)
            ? "https://discord.com/users/" + conn.discord
            : conn.discord != null ? "@" + conn.discord : null;
        return new
        {
            promotionChannelsVisibilityPrivacy = "NoOne",
            twitter = conn.twitter != null
                ? (alwaysReturnUrls ? "https://x.com/" : "@") + conn.twitter
                : null,
            discord = discordUrl,
            telegram = conn.telegram != null
                ? (alwaysReturnUrls ? "https://t.me/" : "@") + conn.telegram
                : null,
            tiktok = conn.tiktok != null
                ? (alwaysReturnUrls ? "https://tiktok.com/@" : "@") + conn.tiktok
                : null,
            youtube = conn.youtube != null
                ? (alwaysReturnUrls ? "https://youtube.com/@" : "@") + conn.youtube
                : null,
            twitch = conn.twitch != null
                ? (alwaysReturnUrls ? "https://twitch.tv/" : "@") + conn.twitch
                : null,
            github = conn.github != null
                ? (alwaysReturnUrls ? "https://github.com/" : "@") + conn.github
                : null,
            roblox = conn.roblox != null
                ? (alwaysReturnUrls ? "https://www.roblox.com/users/profile?username=" : "@") + conn.roblox
                : null,
            // these are always null!
            facebook = (string?) null,
            guilded = (string?) null,
        };
    }

    [HttpPost("promotion-channels")]
    [HttpPostBypass("/v1/promotion-channels")]
    public async Task<dynamic> SetPromotionChannels([Required, FromBody] UserConnections conn)
    {
        // there's prob a better way to do this, maybe a dictionary with a regex of each connection or something
        if (conn == null)
            throw new BadRequestException(2, "The request was empty.");
        if (
            conn.twitter != null &&
            (string.IsNullOrWhiteSpace(conn.twitter) ||
             !Regex.IsMatch(conn.twitter, "^[_A-Za-z][A-Za-z0-9_]{3,14}$"))
            )
            throw new BadRequestException(12, "Twitter handle is invalid.");
        if (
            conn.youtube != null &&
            (string.IsNullOrWhiteSpace(conn.youtube) ||
             !Regex.IsMatch(conn.youtube, "^(?![.-])(?!.*[.]{2})[A-Za-z0-9._-]{3,30}$"))
        )
            throw new BadRequestException(13, "YouTube handle is invalid.");
        if (
            conn.tiktok != null &&
            (string.IsNullOrWhiteSpace(conn.tiktok) ||
             !Regex.IsMatch(conn.tiktok, @"^(?!.*\.\.)(?!.*\.$)[A-Za-z0-9._]{2,24}$"))
        )
            throw new BadRequestException(17, "TikTok handle is invalid.");
        if (
            conn.discord != null && 
            (string.IsNullOrWhiteSpace(conn.discord) ||
             !Regex.IsMatch(conn.discord, "^[a-z0-9._]{2,32}$"))
        )
            throw new BadRequestException(15, "Discord handle is invalid.");
        if (
            conn.telegram != null &&
            (string.IsNullOrWhiteSpace(conn.telegram) ||
             !Regex.IsMatch(conn.telegram, "^[A-Za-z][A-Za-z0-9_]{4,31}$"))
        )
            throw new BadRequestException(16, "Telegram handle is invalid.");
        if (
            conn.twitch != null && 
            (string.IsNullOrWhiteSpace(conn.twitch) ||
             !Regex.IsMatch(conn.twitch, "^[a-z][a-z0-9_]{3,24}$"))
        )
            throw new BadRequestException(14, "Twitch handle is invalid.");
        if (
            conn.github != null && 
            (string.IsNullOrWhiteSpace(conn.github) ||
             !Regex.IsMatch(conn.github, "^(?!-)(?!.*--)[A-Za-z0-9-]{1,39}(?<!-)$"))
        )
            throw new BadRequestException(14, "GitHub handle is invalid.");
        if (
            conn.roblox != null && 
            (string.IsNullOrWhiteSpace(conn.roblox) ||
             !Regex.IsMatch(conn.roblox, "^(?=.{3,20}$)(?!.*__)(?!.*_$)(?!^_)[A-Za-z0-9_]+$"))
        )
            throw new BadRequestException(14, "ROBLOX handle is invalid.");
        
        await services.accountInformation.SetUserConnections(safeUserSession.userId, conn);
        return new {};
    }

    [HttpGet("star-code-affiliates")]
    public dynamic? GetStarCode()
    {
        return null;
    }

    [HttpPost("star-code-affiliates")]
    public void SetStarCode()
    {
        throw new BadRequestException(1, "The code was invalid");
    }
    
    public bool IsDigits(string input) {
        if (string.IsNullOrEmpty(input))
            return false;
        foreach (char c in input) {
            if (!char.IsDigit(c))
                return false;
        }
        return true;
    }

}