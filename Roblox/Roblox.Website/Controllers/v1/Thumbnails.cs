using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Thumbnails;
using Roblox.Exceptions;
using Roblox.Logging;
using Roblox.Models;
using Roblox.Models.Thumbnails;
using Roblox.Services;
using Roblox.Website.WebsiteModels.Thumbnails;
using ServiceProvider = Roblox.Services.ServiceProvider;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/thumbnails/v1")]
public class ThumbnailsControllerV1 : ControllerBase
{
    public static void StartThumbnailFixLoop()
    {
        // this thing is annoying in debug
#if DEBUG
        return;
#endif
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    await ThumbnailFixTask();
                }
                catch (Exception e)
                {
                    Writer.Info(LogGroup.FixBrokenThumbnails, "Failure in Fix: {0}\n{1}",e.Message,e.StackTrace);
                }
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        });
    }
    
    private static async Task ThumbnailFixTask()
    {
        // Debug envs always have one site running, so we don't need a lock
#if !DEBUG
        await using var distributedThumbFixLock =
            await Roblox.Services.Cache.redLock.CreateLockAsync("FixThumbnailsV1", TimeSpan.FromMinutes(10));
        if (!distributedThumbFixLock.IsAcquired)
        {
            Writer.Info(LogGroup.FixBrokenThumbnails, "could not acquire lock for thumb fix");
            return;
        }
        Writer.Info(LogGroup.FixBrokenThumbnails, "Acquired lock for fixing thumbnails");

        try
        {
            await FixAssetThumbnails();
        }
        catch (Exception e)
        {
            Writer.Info(LogGroup.FixBrokenThumbnails, "Error running FixAssetThumbnails(). will move to users. err={0}\n{1}", e.Message, e.StackTrace);
        }

        //Writer.Info(LogGroup.FixBrokenThumbnails,"Fix thumbs for users");
        //await FixUserThumbnails();
        //Writer.Info(LogGroup.FixBrokenThumbnails, "fixed all thumbs");
#endif
    }

    private static async Task FixUserThumbnails()
    {
        using var thumbs = ServiceProvider.GetOrCreate<ThumbnailsService>();
        using var avatar = ServiceProvider.GetOrCreate<AvatarService>();
        
        var brokenIdsPassOne = (await thumbs.GetUserIdsWithBrokenThumbnails()).ToList();
        Writer.Info(LogGroup.FixBrokenThumbnails, "Got users with broken thumbs. Broken = {0}", brokenIdsPassOne.Count);
        foreach (var user in brokenIdsPassOne)
        {
            await Task.Delay(TimeSpan.FromSeconds(5)); // pace ourselves since repairing thumbs isn't as important as rendering new ones
            var t = new Stopwatch();
            t.Start();
            await using var isLocked = await Roblox.Services.Cache.redLock.CreateLockAsync(avatar.GetAvatarRedLockKey(user),
                TimeSpan.FromMinutes(5));
            t.Stop();
            // No acquire means the avatar is still being rendered
            if (!isLocked.IsAcquired) 
                continue;
            t.Stop();
            Writer.Info(LogGroup.FixBrokenThumbnails, "acquired lock for {0} in {1}ms", user, t.ElapsedMilliseconds);
            // Confirm avatar is still broken
            var av = await avatar.GetAvatar(user);
            if (av.headshotUrl != null && av.thumbnailUrl != null) continue;
            Writer.Info(LogGroup.FixBrokenThumbnails, "fix thumbnail for user {0}", user);
            // IgnoreLock because we are already calling function with a lock
            try 
            {
                await avatar.RedrawAvatar(user, null, null, null, true, true);
            }
            catch(Exception e) 
            {
                Writer.Info(LogGroup.FixBrokenThumbnails, "Error fixing user: {0}\n{1}", e.Message, e.StackTrace);
            }
        }
    }

    private static async Task FixAssetThumbnails()
    {
        var services = new ControllerServices();
        // Get all items updated over 5 minutes ago that do not have a thumb
        var itemsToFix = (await services.thumbnails.GetAssetIdsWithoutThumbnail()).ToList();
        //Writer.Info(LogGroup.FixBrokenThumbnails, "Fixing {0} broken thumbnails", itemsToFix.Count);
        
        foreach (var item in itemsToFix)
        {
            await Task.Delay(TimeSpan.FromSeconds(5)); // pace ourselves since repairing thumbs isn't as important as rendering new ones
            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromMinutes(2));
            Writer.Info(LogGroup.FixBrokenThumbnails, "Fixing asset {0}:{1}", item.assetId, item.assetType);
            try
            {
                await services.assets.RenderAssetAsync(item.assetId, item.assetType, cancellation.Token);
            }
            catch (Exception e)
            {
                Writer.Info(LogGroup.FixBrokenThumbnails, "Error fixing asset: {0}\n{1}",e.Message, e.StackTrace);
            }
        }
    }


    [HttpGet("users/avatar-headshot")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetUserHeadshots(string userIds)
    {
        var parsed = userIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = (await services.thumbnails.GetUserHeadshots(parsed)).ToList();
        return new()
        {
            data = result
        };
    }

    [HttpGet("users/avatar")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetUserThumbnails(string userIds)
    {
        var parsed = userIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = await services.thumbnails.GetUserThumbnails(parsed);
        return new()
        {
            data = result
        };
    }
    
    [HttpGet("users/avatar-3d")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetUserThumbnails3D(string userIds)
    {
        var parsed = userIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = (await services.thumbnails.GetUserThumbnails3D(parsed)).ToList();
        _ = Task.WhenAll(result
            .Where(v => v.imageUrl != null)
            .Select(async v =>
                await services.avatar.Update3DRenderModified(v.targetId,
                    Path.GetFileNameWithoutExtension(v.imageUrl!).Replace("_thumbnail3d", "")
                    ))
        );
        foreach (var v in parsed.Except(result.Select(e => e.targetId)).ToList())
        {
            _ = Task.Run(async () => {
                await services.avatar.RedrawAvatar(v);
            });
        }
        return new()
        {
            data = result
        };
    }
    

    [HttpGet("assets")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetAssetThumbnails(string assetIds)
    {
        var parsed = assetIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = await services.thumbnails.GetAssetThumbnails(parsed);
        return new()
        {
            data = result
        };
    }
    
    [HttpGet("users/outfits")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetUserOutfitThumbnails(string userOutfitIds)
    {
        var parsed = userOutfitIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = await services.thumbnails.GetUserOutfitThumbnails(parsed);
        
        return new()
        {
            data = result,
        };
    }
    
    [HttpGet("groups/icons")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetGroupIcons(string groupIds)
    {
        var parsed = groupIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = await services.thumbnails.GetGroupIcons(parsed);
        return new()
        {
            data = result,
        };
    }

    public static async Task<IEnumerable<dynamic>> MultiGetThumbnailsGeneric(List<BatchRequestEntry> thumbs, string type, Func<IEnumerable<long>, Task<IEnumerable<ThumbnailEntry>>> method)
    {
        var idList = thumbs.Where(c => c.type == type).Select(c => c.targetId).ToList();
        if (idList.Count == 0) return Array.Empty<dynamic>();
        var thumbnails = await method(idList);
        return thumbnails.Select(c => new
        {
            requestId = thumbs.Find(v => v.targetId == c.targetId && v.type == type)?.requestId ?? string.Empty,
            targetId = c.targetId,
            state = "Completed",
            imageUrl = Configuration.BaseUrl + c.imageUrl,
            Url = Configuration.BaseUrl + c.imageUrl,
            version = "1" 
        });
    }

    [HttpPost("batch")]
    public async Task<RobloxCollection<dynamic>> BatchThumbnailsRequest(IEnumerable<BatchRequestEntry> request)
    {
        var thumbs = request.ToList();
        var allResults = await Task.WhenAll(new List<Task<IEnumerable<dynamic>>>()
        {
            MultiGetThumbnailsGeneric(thumbs, "AvatarThumbnail", services.thumbnails.GetUserThumbnails),
            MultiGetThumbnailsGeneric(thumbs, "AvatarHeadShot", services.thumbnails.GetUserHeadshots),
            MultiGetThumbnailsGeneric(thumbs, "GameIcon", services.thumbnails.GetUniverseIcons),
            MultiGetThumbnailsGeneric(thumbs, "AssetThumbnail", services.thumbnails.GetAssetThumbnails),
        });
        return new RobloxCollection<dynamic>()
        {
            data = allResults.SelectMany(x => x),
        };
    }

    [HttpGet("games/icons")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetUniverseIcons(string universeIds)
    {
        var parsed = universeIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = await services.thumbnails.GetUniverseIcons(parsed);
        return new()
        {
            data = result.Where(c => c.imageUrl != null).Select(c => new ThumbnailEntry
            {
                targetId = c.targetId,
                imageUrl = Configuration.BaseUrl + c.imageUrl,
                state = c.state,
                version = c.version
            }).ToList()
        };
    }

    [HttpGet("places/gameicons")]
    public async Task<RobloxCollection<ThumbnailEntry>> GetPlaceIcons(string placeIds)
    {
        var parsed = placeIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException();
        var result = await services.thumbnails.GetPlaceIcons(parsed);
        return new()
        {
            data = result.Where(c => c.imageUrl != null).Select(c => new ThumbnailEntry
            {
                targetId = c.targetId,
                imageUrl = Configuration.BaseUrl + c.imageUrl,
                state = c.state,
                version = c.version
            }).ToList()
        };
    }
}