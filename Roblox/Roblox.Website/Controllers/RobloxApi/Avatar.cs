using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Services;
using Roblox.Website.WebsiteModels;
using Roblox.Dto.Avatar;
using Roblox.Exceptions;
using Roblox.Models.Avatar;
using Roblox.Services.App.FeatureFlags;
using Newtonsoft.Json;
using Roblox.Logging;
using ServiceProvider = Roblox.Services.ServiceProvider;
using Roblox.Services.Exceptions;
#pragma warning disable CS8600

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/")]
public class AvatarRBX : ControllerBase
{
    private void FeatureCheck()
    {
        FeatureFlags.FeatureCheck(FeatureFlag.AvatarsEnabled);
    }

    private async void AttemptScheduleRender(bool forceRedraw = false)
    { 
        var userId = safeUserSession.userId;
        if (!forceRedraw)
        {
            using (var cache = ServiceProvider.GetOrCreate<AvatarCache>())
            {
                if (!cache.AttemptScheduleRender(userId))
                {
                    Writer.Info(LogGroup.AvatarService, "Avatar render already scheduled for user {0}", userId);
                    return;
                }

            }
        }

        await Task.Run(async () =>
        {
            //await Task.Delay(TimeSpan.FromSeconds(2));
            Roblox.Models.Avatar.AvatarType? rigType = (Roblox.Models.Avatar.AvatarType?)await services.avatar.GetAvatarType(userId);
            using var cache = ServiceProvider.GetOrCreate<AvatarCache>();
            try
            {
                using var avatarService = Roblox.Services.ServiceProvider.GetOrCreate<AvatarService>();
                var assetIds = await cache.GetPendingAssets(userId);
                var newColors = await cache.GetColors(userId);
                await avatarService.RedrawAvatar(userId, assetIds, newColors, rigType, forceRedraw);
            }
            catch (Exception e)
            {
                Console.WriteLine("Background render failed: {0}\n{1}", e.Message, e.StackTrace);
            }
            finally
            {
                cache.UnscheduleRender(userId);
            }
        });
    }
    [HttpGetBypass("/v1/avatar-fetch")]
    [HttpGetBypass("/v1.1/avatar-fetch")]
    public async Task<IActionResult> CharacterFetch(long? placeId, long userId)
    {
        List<long> accessoryVersionIds = new List<long>();
        List<long> equippedGearVersionIds = new List<long>();
        var wornAssets = await services.avatar.GetWornAssets(userId);
        var avatar = await services.avatar.GetAvatar(userId);
        var assetInfo = await services.assets.MultiGetInfoById(wornAssets);
        dynamic bodyColors = new
        {
            headColorId = avatar.headColorId,
            leftArmColorId = avatar.leftArmColorId,
            leftLegColorId = avatar.leftLegColorId,
            rightArmColorId = avatar.rightArmColorId,
            rightLegColorId = avatar.rightLegColorId,
            torsoColorId = avatar.torsoColorId,

            HeadColor = avatar.headColorId,
            LeftArmColor = avatar.leftArmColorId,
            LeftLegColor = avatar.leftLegColorId,
            RightArmColor = avatar.rightArmColorId,
            RightLegColor = avatar.rightLegColorId,
            TorsoColor = avatar.torsoColorId
        };
        // why the fuck are there capitalized and not capitalized, super ugly
        dynamic scales = new 
        {
            avatar.scales.height,
            Height = avatar.scales.height,
            avatar.scales.width,
            Width = avatar.scales.width,
            avatar.scales.head,
            Head = avatar.scales.head,
            avatar.scales.depth,
            Depth = avatar.scales.depth,
            avatar.scales.proportion,
            Proportion = avatar.scales.proportion,
            avatar.scales.bodyType,
            BodyType = avatar.scales.bodyType
        };
        
        equippedGearVersionIds.AddRange(assetInfo.Where(d => d.assetType == Models.Assets.Type.Gear).Select(d => d.id));
        accessoryVersionIds.AddRange(assetInfo.Where(d => (d.assetType != Models.Assets.Type.Gear && placeId != 0) && d.assetType != Models.Assets.Type.EmoteAnimation).Select(d => d.id));
        if (placeId != 0)
        {
            equippedGearVersionIds = new List<long>();
        }
        int positionCounter = 1;
        var animationAssetIds = assetInfo
            .Where(c => c.assetType == Models.Assets.Type.RunAnimation 
                    || c.assetType == Models.Assets.Type.JumpAnimation 
                    || c.assetType == Models.Assets.Type.FallAnimation 
                    || c.assetType == Models.Assets.Type.ClimbAnimation
                    || c.assetType == Models.Assets.Type.IdleAnimation
                    || c.assetType == Models.Assets.Type.WalkAnimation
                    || c.assetType == Models.Assets.Type.SwimAnimation)
            .GroupBy(c => c.assetType.ToString().Replace("Animation", "").ToLower())
            .ToDictionary(
                g => g.Key,
                g => g.First().id 
            );

        var result = new 
        {
            resolvedAvatarType = avatar.avatarType.ToString(),
            accessoryVersionIds,
            equippedGearVersionIds,
            assetAndAssetTypeIds = assetInfo
                .Where(c => c.assetType != Models.Assets.Type.EmoteAnimation 
                            && !animationAssetIds.ContainsKey(c.assetType.ToString().Replace("Animation", "").ToLower()))
                .Select(c => new
                {
                    assetId = c.id,
                    assetTypeId = (int)c.assetType,
            }),
            backpackGearVersionIds = equippedGearVersionIds,
            animationAssetIds = animationAssetIds,
            playerAvatarType = avatar.avatarType.ToString(),
            scales,
            bodyColorsUrl = $"{Configuration.BaseUrl}/Asset/BodyColors.ashx?userId={userId}",
            bodyColors,
            emotes = assetInfo.Where(c => c.assetType == Models.Assets.Type.EmoteAnimation).Select(c => new
            {
                assetId = c.id,
                assetName = c.name,
                position = positionCounter++,
            }),
        };

        string jsonString = JsonConvert.SerializeObject(result);
        return Content(jsonString, "application/json");
    }

    [HttpPostBypass("v1/avatar/redraw-thumbnail")]
    public void RequestRedrawAvatar()
    {
        FeatureCheck();
        AttemptScheduleRender(true);
    }

    [HttpPostBypass("v1/avatar/set-wearing-assets")]
    public async Task SetWornAssets([Required, FromBody] SetWearingAssetsRequest request)
    {
        FeatureCheck();

        using var cache = ServiceProvider.GetOrCreate<AvatarCache>();
        await cache.SetPendingAssets(safeUserSession.userId, request.assetIds);

        
        AttemptScheduleRender();
    }

    [HttpPostBypass("v1/avatar/set-body-colors")]
    public async Task SetBodyColors([Required, FromBody] SetColorsRequest colors)
    {
        FeatureCheck();

        using var cache = ServiceProvider.GetOrCreate<AvatarCache>();
        await cache.SetColors(safeUserSession.userId, colors);

        AttemptScheduleRender();
    }

    [HttpGetBypass("v1/users/{userId:long}/outfits")]
    public async Task<dynamic> GetUserOutfits(long userId, int itemsPerPage, int page)
    {
        FeatureCheck();
        var offset = itemsPerPage * page - itemsPerPage;
        var result = (await services.avatar.GetUserOutfits(userId, itemsPerPage, offset)).ToList();
        return new
        {
            filteredCount = 0,
            data = result,
            total = result.Count,
        };
    }

    [HttpPostBypass("v1/outfits/{outfitId:long}/wear")]
    public async Task WearOutfit(long outfitId)
    {
        FeatureCheck();
        var outfitDetails = await services.avatar.GetOutfitById(outfitId);
        await services.avatar.RedrawAvatar(safeUserSession.userId, outfitDetails.assetIds, outfitDetails.details);
    }

    /// <summary>
    /// Create an outfit
    /// </summary>
    /// <remarks>
    /// Unlike Roblox, this method ignores the body parameters - it just uses the outfit of the authenticated user.
    /// </remarks>
    [HttpPostBypass("v1/outfits/create")]
    public async Task CreateOutfit([Required,FromBody] CreateOutfitRequest request)
    {
        FeatureCheck();
        var assets = await services.avatar.GetWornAssets(safeUserSession.userId);
        var existingAvatar = await services.avatar.GetAvatar(safeUserSession.userId);
        await services.avatar.CreateOutfit(safeUserSession.userId, request.name, existingAvatar.thumbnailUrl,
            existingAvatar.headshotUrl, new OutfitExtendedDetails()
            {
                details = new OutfitAvatar
                {
                    headColorId = existingAvatar.headColorId,
                    torsoColorId = existingAvatar.torsoColorId,
                    leftArmColorId = existingAvatar.leftArmColorId,
                    rightArmColorId = existingAvatar.rightArmColorId,
                    leftLegColorId = existingAvatar.leftLegColorId,
                    rightLegColorId = existingAvatar.rightLegColorId,
                    height = existingAvatar.scales.height,
                    width = existingAvatar.scales.width,
                    head = existingAvatar.scales.head,
                    depth = existingAvatar.scales.depth,
                    proportion = existingAvatar.scales.proportion,
                    bodyType = existingAvatar.scales.bodyType,
                    avatarType = existingAvatar.avatarType,
                    userId = safeUserSession.userId,
                },
                assetIds = assets,
            });
    }

    [HttpPostBypass("v1/outfits/{outfitId:long}/delete")]
    public async Task DeleteOutfit(long outfitId)
    {
        FeatureCheck();
        var info = await services.avatar.GetOutfitById(outfitId);
        if (info.details.userId != safeUserSession.userId)
            throw new ForbiddenException(0, "Forbidden");

        await services.avatar.DeleteOutfit(outfitId);
    }

    [HttpPost("outfits/{outfitId:long}/rename")]
    public async Task RenameOutfit(long outfitId, [Required,FromBody] UpdateOutfitRequest request)
    {
        FeatureCheck();
        if (request.name == null) throw new BadRequestException(0, "Name field required in body");
        var outfitDetails = await services.avatar.GetOutfitById(outfitId);
        if (outfitDetails.details.userId != safeUserSession.userId)
            throw new ForbiddenException();
        await services.avatar.RenameOutfit(outfitId, request.name);
    }
    
    /// <summary>
    /// Update an outfit
    /// </summary>
    /// <remarks>
    /// Unlike Roblox, this method ignores the body parameters - it just uses the outfit of the authenticated user.
    /// </remarks>
    [HttpPatch("outfits/{outfitId:long}")]
    public async Task UpdateOutfit(long outfitId, [Required,FromBody] UpdateOutfitRequest request)
    {
        FeatureCheck();
        var outfitDetails = await services.avatar.GetOutfitById(outfitId);
        if (outfitDetails.details.userId != safeUserSession.userId)
            throw new ForbiddenException();
        var assets = await services.avatar.GetWornAssets(safeUserSession.userId);
        var existingAvatar = await services.avatar.GetAvatar(safeUserSession.userId);
        await services.avatar.UpdateOutfit(outfitId, request.name, existingAvatar.thumbnailUrl,
            existingAvatar.headshotUrl, new OutfitExtendedDetails()
            {
                details = new OutfitAvatar()
                {
                    headColorId = existingAvatar.headColorId,
                    torsoColorId = existingAvatar.torsoColorId,
                    leftArmColorId = existingAvatar.leftArmColorId,
                    rightArmColorId = existingAvatar.rightArmColorId,
                    leftLegColorId = existingAvatar.leftLegColorId,
                    rightLegColorId = existingAvatar.rightLegColorId,
                    height = existingAvatar.scales.height,
                    width = existingAvatar.scales.width,
                    head = existingAvatar.scales.head,
                    depth = existingAvatar.scales.depth,
                    proportion = existingAvatar.scales.proportion,
                    bodyType = existingAvatar.scales.bodyType,
                    avatarType = existingAvatar.avatarType,
                    userId = safeUserSession.userId,
                },
                assetIds = assets,
            });
    }

    [HttpGetBypass("v1/users/{userId:long}/avatar")]
    public async Task<dynamic> GetAvatar(long userId)
    {
        var assets = await services.avatar.GetWornAssets(userId);
        var multiGetResults = await services.assets.MultiGetInfoById(assets);
        var existingAvatar = await services.avatar.GetAvatar(userId);
        // If we dont have an avatar then its most likely a game loading a avatar
        // try
        // {
        //     existingAvatar = await services.avatar.GetAvatar(userId);
        // }
        // catch (RecordNotFoundException)
        // {
        //     return Redirect("https://avatar.roblox.com/v1/avatar-fetch?userId=" + userId);
        // }

        return new
        {
            scales = new
            {
                existingAvatar.scales.height,
                existingAvatar.scales.width,
                existingAvatar.scales.head,
                existingAvatar.scales.depth,
                existingAvatar.scales.proportion,
                existingAvatar.scales.bodyType,
            },
            playerAvatarType = existingAvatar.avatarType.ToString(),
            bodyColors = (ColorEntry)existingAvatar,
            assets = multiGetResults.Select(c =>
            {
                return new
                {
                    id = c.id,
                    name = c.name,
                    assetType = new
                    {
                        id = (int) c.assetType,
                        name = c.assetType,
                    },
                    currentVersionId = c.id,
                };
            }),
        };
    }

    [HttpGetBypass("v1/avatar")]
    public async Task<dynamic> GetMyAvatar()
    {
        return await GetAvatar(userSession?.userId ?? (long.TryParse(HttpContext.Request.Cookies["USERID"], out var userId) ? userId : 1));
    }

    [HttpGetBypass("v1/avatar/metadata")]
    public dynamic GetAvatarMetadata()
    {
        return new
        {
            enableDefaultClothingMessage = false,
            isAvatarScaleEmbeddedInTab = true,
            isBodyTypeScaleOutOfTab = true,
            scaleHeightIncrement = 0.05,
            scaleWidthIncrement = 0.05,
            scaleHeadIncrement = 0.05,
            scaleProportionIncrement = 0.05,
            scaleBodyTypeIncrement = 0.05,
            supportProportionAndBodyType = true,
            showDefaultClothingMessageOnPageLoad = false,
            areThreeDeeThumbsEnabled = true,
        };
    }

    [HttpGetBypass("v1/avatar-rules")]
    public dynamic GetAvatarRules()
    {
        return new
        {
            playerAvatarTypes = Enum.GetNames<AvatarType>(),
            scales = new
            {
                height = new
                {
                    min = 0.9,
                    max = 1.05,
                    increment = 0.01,
                },
                width = new
                {
                    min = 0.7,
                    max = 1.0,
                    increment = 0.01,
                },
                head = new
                {
                    min = 0.9,
                    max = 1.0,
                    increment = 0.01,
                },
                proportion = new
                {
                    min = 0.0,
                    max = 1.0,
                    increment = 0.01,
                },
                bodyType = new
                {
                    min = 0.0,
                    max = 1.0,
                    increment = 0.01,
                },
            },
            wearableAssetTypes = new List<dynamic>()
            {
                new { maxNumber = 3, id = 8, name = "Hat" },
                new { maxNumber = 1, id = 41, name = "Hair Accessory" },
                new { maxNumber = 1, id = 42, name = "Face Accessory" },
                new { maxNumber = 1, id = 43, name = "Neck Accessory" },
                new { maxNumber = 1, id = 44, name = "Shoulder Accessory" },
                new { maxNumber = 1, id = 45, name = "Front Accessory" },
                new { maxNumber = 1, id = 46, name = "Back Accessory" },
                new { maxNumber = 1, id = 47, name = "Waist Accessory" },
                new { maxNumber = 1, id = 18, name = "Face" },
                new { maxNumber = 1, id = 19, name = "Gear" },
                new { maxNumber = 1, id = 17, name = "Head" },
                new { maxNumber = 1, id = 29, name = "Left Arm" },
                new { maxNumber = 1, id = 30, name = "Left Leg" },
                new { maxNumber = 1, id = 12, name = "Pants" },
                new { maxNumber = 1, id = 28, name = "Right Arm" },
                new { maxNumber = 1, id = 31, name = "Right Leg" },
                new { maxNumber = 1, id = 11, name = "Shirt" },
                new { maxNumber = 1, id = 2, name = "T-Shirt" },
                new { maxNumber = 1, id = 27, name = "Torso" },
                new { maxNumber = 1, id = 48, name = "Climb Animation" },
                new { maxNumber = 1, id = 49, name = "Death Animation" },
                new { maxNumber = 1, id = 50, name = "Fall Animation" },
                new { maxNumber = 1, id = 51, name = "Idle Animation" },
                new { maxNumber = 1, id = 52, name = "Jump Animation" },
                new { maxNumber = 1, id = 53, name = "Run Animation" },
                new { maxNumber = 1, id = 54, name = "Swim Animation" },
                new { maxNumber = 1, id = 55, name = "Walk Animation" },
                new { maxNumber = 1, id = 56, name = "Pose Animation" },
                new { maxNumber = 0, id = 61, name = "Emote Animation" },
            },
            bodyColorsPalette = Roblox.Models.Avatar.AvatarMetadata.GetColors(),
            basicBodyColorsPalette = new List<dynamic>()
            {
              new { brickColorId = 364, hexColor = "#5A4C42", name = "Dark taupe" },
				new { brickColorId = 217, hexColor = "#7C5C46", name = "Brown" },
				new { brickColorId = 359, hexColor = "#AF9483", name = "Linen" },
				new { brickColorId = 18, hexColor = "#CC8E69", name = "Nougat" },
				new {
					brickColorId = 125,
					hexColor = "#EAB892",
					name = "Light orange",
				},
				new { brickColorId = 361, hexColor = "#564236", name = "Dirt brown" },
				new {
					brickColorId = 192,
					hexColor = "#694028",
					name = "Reddish brown",
				},
				new { brickColorId = 351, hexColor = "#BC9B5D", name = "Cork" },
				new { brickColorId = 352, hexColor = "#C7AC78", name = "Burlap" },
				new { brickColorId = 5, hexColor = "#D7C59A", name = "Brick yellow" },
				new { brickColorId = 153, hexColor = "#957977", name = "Sand red" },
				new { brickColorId = 1007, hexColor = "#A34B4B", name = "Dusty Rose" },
				new { brickColorId = 101, hexColor = "#DA867A", name = "Medium red" },
				new {
					brickColorId = 1025,
					hexColor = "#FFC9C9",
					name = "Pastel orange",
				},
				new {
					brickColorId = 330,
					hexColor = "#FF98DC",
					name = "Carnation pink",
				},
				new { brickColorId = 135, hexColor = "#74869D", name = "Sand blue" },
				new { brickColorId = 305, hexColor = "#527CAE", name = "Steel blue" },
				new { brickColorId = 11, hexColor = "#80BBDC", name = "Pastel Blue" },
				new {
					brickColorId = 1026,
					hexColor = "#B1A7FF",
					name = "Pastel violet",
				},
				new { brickColorId = 321, hexColor = "#A75E9B", name = "Lilac" },
				new {
					brickColorId = 107,
					hexColor = "#008F9C",
					name = "Bright bluish green",
				},
				new { brickColorId = 310, hexColor = "#5B9A4C", name = "Shamrock" },
				new { brickColorId = 317, hexColor = "#7C9C6B", name = "Moss" },
				new { brickColorId = 29, hexColor = "#A1C48C", name = "Medium green" },
				new {
					brickColorId = 105,
					hexColor = "#E29B40",
					name = "Br. yellowish orange",
				},
				new {
					brickColorId = 24,
					hexColor = "#F5CD30",
					name = "Bright yellow",
				},
				new {
					brickColorId = 334,
					hexColor = "#F8D96D",
					name = "Daisy orange",
				},
				new {
					brickColorId = 199,
					hexColor = "#635F62",
					name = "Dark stone grey",
				},
				new { brickColorId = 1002, hexColor = "#CDCDCD", name = "Mid gray" },
				new {
					brickColorId = 1001,
					hexColor = "#F8F8F8",
					name = "Institutional white",
				},
            },
            minimumDeltaEBodyColorDifference = 11.4,
            defaultClothingAssetLists = new
            {
                defaultShirtAssetIds = new List<long>() {1,2},
                defaultPantAssetIds = new List<long>() {1,2},
            },
            bundlesEnabledForUser = false,
            emotesEnabledForUser = false,
        };
    }
}