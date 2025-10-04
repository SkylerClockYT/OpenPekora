using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roblox.Dto.Thumbnails;
using Roblox.Exceptions;
using Roblox.Models;
using Roblox.Models.Thumbnails;
using Roblox.Website.WebsiteModels.Thumbnails;

namespace Roblox.Website.Controllers;
[ApiController]
[Route("/")]
public class RbxThumbnails : ControllerBase
{
    public enum ThumbnailType
    {
        UserHeadshot = 1,
        UserAvatar,
        Asset,
        PlaceIcon
    }
    private async Task<RedirectResult> GetThumbnailUrl(long id, ThumbnailType type)
    {
        List<ThumbnailEntry> result = new List<ThumbnailEntry>();
        
        switch (type)
        {

            case ThumbnailType.UserHeadshot:
                result = (await services.thumbnails.GetUserHeadshots(new[] { id })).ToList();
                break;
            case ThumbnailType.UserAvatar:
                result = (await services.thumbnails.GetUserThumbnails(new[] { id })).ToList();
                break;
            case ThumbnailType.Asset:
                result = (await services.thumbnails.GetAssetThumbnails(new[] { id })).ToList();
                break;
            case ThumbnailType.PlaceIcon:
                result = (await services.thumbnails.GetPlaceIcons(new[] { id })).ToList();
                return new RedirectResult((Configuration.BaseUrl + result.FirstOrDefault()?.imageUrl) ?? "/img/placeholder.png", false);
        }

        var imageUrl = result.FirstOrDefault()?.imageUrl ?? "/img/placeholder.png";
        return new RedirectResult(imageUrl, false);
    }
    //avatar stuff
    [HttpGetBypass("avatar-thumbnail/image")]
    [HttpGetBypass("thumbs/avatar.ashx")]
    public async Task<RedirectResult> GetAvatarThumbnail(long userId, string? username)
    {
        if (username != null)
        {
            try
            {
                userId = await services.users.GetUserIdFromUsername(username);
            }
            catch (Exception)
            {
                return new RedirectResult("/img/blocked.png", false);
            }
        }
        return await GetThumbnailUrl(userId, ThumbnailType.UserAvatar);
    }
    //headshot stuff
    [HttpGetBypass("Thumbs/Head.ashx")]
    [HttpGetBypass("headshot-thumbnail/image")]
    [HttpGetBypass("thumbs/avatar-headshot.ashx")]
    public async Task<RedirectResult> GetAvatarHeadShot(long userId)
    {
        return await GetThumbnailUrl(userId, ThumbnailType.UserHeadshot);
    }
    //place icon
    [HttpGetBypass("Thumbs/PlaceIcon.ashx")]
    [HttpGetBypass("Thumbs/GameIcon.ashx")]
    public async Task<RedirectResult> GetGameIcon(long assetId)
    {
        return await GetThumbnailUrl(assetId, ThumbnailType.PlaceIcon);
    }
    //asset icon stuff
    [HttpGet("asset-thumbnail/image")]
    [HttpGetBypass("icons/asset.ashx")]
    [HttpGetBypass("Game/Tools/ThumbnailAsset.ashx")]
    [HttpGetBypass("thumbs/asset.ashx")]
    public async Task<RedirectResult> GetAssetThumbnail(long assetId, long? aid)
    {
        if(aid != null)
            assetId = (long)aid;
        return await GetThumbnailUrl(assetId, ThumbnailType.Asset);
    }
    //all json thumbnail apis
    [HttpGetBypass("avatar-thumbnail/json")]
    public async Task<dynamic> GetAvatarThumbnailJson([Required] long userId)
    {
        var result = (await services.thumbnails.GetUserThumbnails(new[] {userId})).ToList();
        return new
        {
            Url = $"{Configuration.BaseUrl}{result[0].imageUrl}",
            Final = true,
            SubstitutionType = 0
        };
    }
    
    [HttpGetBypass("avatar-thumbnail-3d/json")]
    public async Task<dynamic> GetAvatarThumbnail3DJson([Required] long userId) 
    {
        // if (userId == 62022330) userId = 3; avatar page testing
        var result = (await services.thumbnails.GetUserThumbnails3D(new[] {userId})).ToList();
        var imageUrl = result.Count > 0 ? result[0].imageUrl : null;
        if (imageUrl != null)
        {
            await services.avatar.Update3DRenderModified(userId, Path.GetFileNameWithoutExtension(imageUrl).Replace("_thumbnail3d", ""));
        }

        return new
        {
            Url = imageUrl != null ? $"{Configuration.BaseUrl}{imageUrl}" : null,
            Final = true,
            SubstitutionType = 0
        };
    }

    [HttpGetBypass("thumbnail/avatar-headshot")]
    public async Task<dynamic> GetHeadshotThumbnailJson(long userId)
    {
        var result = (await services.thumbnails.GetUserHeadshots(new[] { userId })).ToList();
        return new
        {
            Final = true,
            Url = $"{Configuration.BaseUrl}{result[0].imageUrl}",
        };
    }

    [HttpGetBypass("asset-thumbnail/json")]
    public async Task<dynamic> GetAssetThumbnailJson([Required] long assetId)
    {
        var result = (await services.thumbnails.GetAssetThumbnails(new[] {assetId})).ToList();
        return new
        {
            Url = $"{Configuration.BaseUrl}{result[0].imageUrl}",
            Final = true,
            SubstitutionType = 0
        };
    }

    [HttpGetBypass("asset-gameicon/multiget")]
    public async Task<dynamic> GetGameIconMultiGet([FromQuery] List<long> universeId)
    {
        // var gameIcons = await services.thumbnails.GetGameIcons(universeId);
        // return new ThumbnailEntryRBX()
        // {
        //     //targetId = c.targetId,
        //     TargetId = c.targetId,
        //     Url = c.imageUrl,
        //     State = c.imageUrl == null ? ThumbnailState.Pending : c.moderationStatus == ModerationStatus.Declined ? ThumbnailState.Blocked : ThumbnailState.Completed,
        // };
        return await services.thumbnails.GetGameIconsRBX(universeId);
    }
    [HttpGetBypass("v1/games/icons")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetGameIcons(string universeIds)
    {
        var parsed = universeIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = await services.thumbnails.GetUniverseIcons(parsed);
        var result2 = result.Select(thumbnail =>
            new ThumbnailEntry
            {
                targetId = thumbnail.targetId,
                imageUrl = Configuration.BaseUrl + thumbnail.imageUrl,
                state = ThumbnailState.Completed,
            }).ToList();
        return new()
        {
            data = result2,
        };
    }
    [HttpGet("v1/users/avatar-headshot")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetMultiHeadshot(string userIds)
    {
        var parsed = userIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = (await services.thumbnails.GetUserHeadshots(parsed)).ToList();

        foreach (var item in result)
        {
            if (item.imageUrl is null) continue;

            item.imageUrl = Configuration.BaseUrl + item.imageUrl;
        }
        var result2 = result.ToList();
        return new()
        {
            data = result2,
        };
    }

    public static async Task<IEnumerable<dynamic>> MultiGetThumbnailsGeneric(List<BatchRequestEntry> thumbs, string type, Func<IEnumerable<long>, Task<IEnumerable<ThumbnailEntry>>> method)
    {
        var idList = thumbs.Where(c => c.type == type).Select(c => c.targetId).ToList();
        if (idList.Count == 0) return Array.Empty<dynamic>();

        var thumbnails = await method(idList);
        var results = new List<dynamic>();

        foreach (var c in thumbnails)
        {
            if (!string.IsNullOrEmpty(c.imageUrl))
            {
                results.Add(new
                {
                    requestId = thumbs.Find(v => v.targetId == c.targetId && v.type == type)?.requestId ?? string.Empty,
                    errorCode = 0,
                    errorMessage = string.Empty,
                    targetId = c.targetId,
                    state = c.state,
                    imageUrl = Configuration.BaseUrl + c.imageUrl,
                    version = c.version
                });
            }
            else
            {
                results.Add(new
                {
                    requestId = thumbs.Find(v => v.targetId == c.targetId && v.type == type)?.requestId ?? string.Empty,
                    errorCode = 4,
                    errorMessage = "The requested Ids are invalid, of an invalid type or missing.",
                    targetId = c.targetId,
                    state = ThumbnailState.Error,
                    imageUrl = (string?)null,
                    version = (string?)null
                });
            }
        }

        return results;
    }

    [HttpPostBypass("v1/batch")]
    public async Task<dynamic> BatchThumbnailsRequest([FromBody] IEnumerable<BatchRequestEntry> request)
    {
        var thumbs = request.ToList();
        var allResults = await Task.WhenAll(new List<Task<IEnumerable<dynamic>>>()
        {
            MultiGetThumbnailsGeneric(thumbs, "Avatar", services.thumbnails.GetUserThumbnails),
            MultiGetThumbnailsGeneric(thumbs, "AvatarThumbnail", services.thumbnails.GetUserThumbnails),
            MultiGetThumbnailsGeneric(thumbs, "AvatarHeadShot", services.thumbnails.GetUserHeadshots),
            MultiGetThumbnailsGeneric(thumbs, "AvatarBust", services.thumbnails.GetUserHeadshots),
            MultiGetThumbnailsGeneric(thumbs, "GameIcon", services.thumbnails.GetUniverseIcons),
            MultiGetThumbnailsGeneric(thumbs, "AutoGeneratedGameIcon", services.thumbnails.GetUniverseIcons),
            MultiGetThumbnailsGeneric(thumbs, "ForceAutoGeneratedGameIcon", services.thumbnails.GetUniverseIcons),
            MultiGetThumbnailsGeneric(thumbs, "GameThumbnail", services.thumbnails.GetAssetThumbnails),
            MultiGetThumbnailsGeneric(thumbs, "Asset", services.thumbnails.GetAssetThumbnails),
            MultiGetThumbnailsGeneric(thumbs, "AssetThumbnail", services.thumbnails.GetAssetThumbnails),
            MultiGetThumbnailsGeneric(thumbs, "AutoGeneratedAsset", services.thumbnails.GetPlaceIcons),
            MultiGetThumbnailsGeneric(thumbs, "GroupIcon", services.thumbnails.GetGroupIcons),
        });

        return new RobloxCollection<dynamic>()
        {
            data = allResults.SelectMany(result => result).ToList()
        };
    }
}

