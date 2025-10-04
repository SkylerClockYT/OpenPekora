using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Economy;
using Roblox.Services.Exceptions;
using Roblox.Models;
using Roblox.Dto.Games;
using Roblox.Exceptions;
using Roblox.Models.Assets;
using Roblox.Models.Db;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/badges/v1/")]
public class BadgesControllerV1 : ControllerBase
{
    // base: https://apidocs.sixteensrc.zip/badges/docs.html#/
    
    // Gets badge information by the badge id.
    [HttpGet("badges/{badgeId:long}")]
    [HttpGetBypass("/v1/badges/{badgeId:long}")]
    [HttpPost("badges/{badgeId:long}")]
    [HttpPostBypass("/v1/badges/{badgeId:long}")]
    public async Task<BadgeAssetDetails> GetBadgeDetails(long badgeId) 
    {
        // TODO: is this even needed?
        var basicBadgeInfo = await services.badges.GetBadgeInfo(badgeId);
        if (basicBadgeInfo is null) {
            throw new BadRequestException(0, "Badge is invalid or does not exist");
        }

        var uni = await services.games.GetUniverseInfo(basicBadgeInfo.universeId);
        // no need to check if it's null right?
        var badgeInfo = await services.badges.GetBadgeInfoExtended(badgeId, uni, 1, 0, null);
        
        return badgeInfo.First();
    }
    
    // Updates badge configuration.
    [HttpPatch("badges/{badgeId:long}")]
    public async Task<dynamic> UpdateBadgeConfig(long badgeId, [Required, FromBody] BadgeUpdateRequest request) 
    {
        await services.assets.ValidatePermissions(badgeId, safeUserSession.userId);
        
        var basicBadgeInfo = await services.badges.GetBadgeInfo(badgeId);
        if (basicBadgeInfo is null) 
            throw new BadRequestException(0, "Badge is invalid or does not exist");
        

        await services.assets.EnsureAssetIsModerated(badgeId);
        await services.badges.UpdateBadge(badgeId, request.enabled);
        await services.assets.UpdateAsset(badgeId);
        return new { };
    }
    
    // Gets badge by their awarding game.
    [HttpGet("universes/{universeId:long}/badges")]
    [HttpGetBypass("/v1/universes/{universeId:long}/badges")]
    public async Task<RobloxCollectionPaginated<BadgeAssetDetails>> GetUniverseBadges(long universeId, int limit, string? cursor, SortOrder? sortOrder)
    {
        if (limit is > 100 or < 1) limit = 10;
        var offset = cursor != null ? int.Parse(cursor) : 0;
        var uni = await services.games.GetUniverseInfo(universeId);
        var badgeInfo = (await services.badges.GetBadgesForUniverse(uni, limit, offset, sortOrder)).ToList();
        
        return new RobloxCollectionPaginated<BadgeAssetDetails>()
        {
            previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
            nextPageCursor = badgeInfo.Count() >= limit ? (offset + limit).ToString() : null,
            data = badgeInfo,
        };
    }
    
    // Gets a list of badges a user has been awarded.
    [HttpGet("users/{userId:long}/badges")]
    [HttpGetBypass("/v1/users/{userId:long}/badges")]
    public async Task<RobloxCollectionPaginated<BadgeAssetDetails>> GetBadges(long userId, int limit, string? cursor, SortOrder? sortOrder)
    {
        if (limit is > 100 or < 1) limit = 10;
        var offset = cursor != null ? int.Parse(cursor) : 0;
        var badgeInfo = (await services.badges.GetBadgesForUser(userId, limit, offset, sortOrder)).ToList();
        
        return new RobloxCollectionPaginated<BadgeAssetDetails>()
        {
            previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
            nextPageCursor = badgeInfo.Count() >= limit ? (offset + limit).ToString() : null,
            data = badgeInfo,
        };
    }
    
    // Gets timestamps for when badges were awarded to a user.
    [HttpGet("users/{userId:long}/badges/awarded-dates")]
    [HttpGetBypass("/v1/users/{userId:long}/badges/awarded-dates")]
    public async Task<dynamic> GetBadgeTimestamps(long userId, string badgeIds)
    {
        var ids = badgeIds.Split(",").Select(long.Parse).ToArray();
        if (!ids.Any())
            return Array.Empty<BadgeAwardDate>();
        return new
        {
            data = await services.badges.GetUserBadgeAwardedDates(userId, ids),
        };
    }
    
    // Award a badge to a user.
    [HttpPost("users/{userId:long}/badges/{badgeId:long}/award-badge")]
    [HttpPostBypass("/v1/users/{userId:long}/badges/{badgeId:long}/award-badge")]
    [HttpPostBypass("/assets/award-badge")]
    public async Task<dynamic> AwardBadge(long userId, long badgeId, long? placeId)
    {
        if (!isRCC) {
            throw new PermissionException(badgeId, safeUserSession.userId);
        }

        if (placeId is null) {
            var robloxPlaceId = Request.Headers["Roblox-Place-Id"].ToString();
            if (!long.TryParse(robloxPlaceId, out _)) {
                throw new BadRequestException(0, "Missing Roblox-Place-Id Header");
            }
            placeId = long.Parse(robloxPlaceId);
        }

        if (userId is 0) {
            // attempt to pull from query, in accordance to assets/award-badge
            if (string.IsNullOrEmpty(Request.Query["userId"].ToString()) || !long.TryParse(Request.Query["userId"], out _))
                throw new BadRequestException(0, "User does not exist.");
            userId = long.Parse(Request.Query["userId"]!);
        }
        
        if (badgeId is 0) {
            // attempt to pull from query, in accordance to assets/award-badge
            if (string.IsNullOrEmpty(Request.Query["badgeId"].ToString()) || !long.TryParse(Request.Query["badgeId"], out _))
                throw new BadRequestException(0, "Badge does not exist.");
            badgeId = long.Parse(Request.Query["badgeId"]!);
        }

        // checks if userId is an actual user
        var user = await services.users.GetUserById(userId);
        var universeId = await services.games.GetUniverseId(placeId.Value);
        // shouldnt have to check null cuz of above right?
        var uni = await services.games.GetUniverseInfo(universeId);
        var badgeInfo = await services.badges.GetBadgeInfo(badgeId);

        if (badgeInfo is null) 
            throw new BadRequestException(0, "Badge is invalid or does not exist");
        
        if (!badgeInfo.enabled)
            throw new BadRequestException(8, "The badge is disabled.");

        if (badgeInfo.universeId != universeId)
            throw new ForbiddenException(8, "The place doesn't have permission to award the badge.");

        if ((await services.users.GetUserAssets(userId, badgeId)).Any())
            throw new BadRequestException(0, "User already owns the badge");
        // TODO: put proper error code here from apidocs sixteensrc 
        
        await services.assets.IncrementSaleCount(badgeId);
        await services.users.CreateUserAsset(userId, badgeId);
        // await services.assets.IncrementAssetSales(badgeId);
        
        if (Request.Path == "/assets/award-badge") 
        {
            var badgeProd = await services.assets.GetAssetCatalogInfo(badgeId);
            return $"{user.username} won {badgeProd.creatorName}'s {badgeProd.name} award! :3";
        }
        
        return new 
        {
            creatorType = uni.creator.type,
            creatorId = uni.creator.id,
            awardAssetIds = Array.Empty<dynamic>()
        };
    }
    
    // Removes a badge from a user.
    [HttpDelete("users/{userId:long}/badges/{badgeId:long}")]
    public async Task<dynamic> RemoveBadgeFromUser(long userId, long badgeId)
    {
        if (!isRCC) {
            throw new PermissionException(badgeId, safeUserSession.userId);
        }
        
        // checks if userId is an actual user
        await services.users.GetUserById(userId);
        var badgeInfo = await services.badges.GetBadgeInfo(badgeId);
        if (badgeInfo is null) {
            throw new BadRequestException(0, "Badge is invalid or does not exist");
        }
        
        // TODO: check if this is necessary
        // might be necessary?
        // if ((await services.users.GetUserAssets(userId, badgeId)).Any()) {
        //     await services.users.DeleteUserAsset(userId, badgeId);
        // }
        await services.users.DeleteUserAsset(userId, badgeId);
        
        return new {};
    }
    
    // Removes a badge from the authenticated user.
    // [HttpDelete("users/badges/{badgeId:long}")]
    // public async Task<dynamic> RemoveBadgeFromSelf(long badgeId)
    // {
        // TODO: is this safe?
        // var userId = safeUserSession.userId;
        //
        // var badgeInfo = await services.badges.GetBadgeInfo(badgeId);
        // if (badgeInfo is null) {
        //     throw new BadRequestException(0, "Badge is invalid or does not exist");
        // }
        //
        // await services.users.DeleteUserAsset(userId, badgeId);
        
    //     return new {};
    // }
}