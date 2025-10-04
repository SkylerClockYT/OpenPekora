using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CsvHelper;
using Dapper;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Roblox.Dto;
using Roblox.Dto.Assets;
using Roblox.Dto.Avatar;
using Roblox.Models.Assets;
using Roblox.Models.Avatar;
using Roblox.Services.DbModels;
using Roblox.Exceptions.Services.Users;
using Roblox.Logging;
using Roblox.Rendering;
using Roblox.Services.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using AssetId = Roblox.Services.DbModels.AssetId;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Services;

public class AvatarService : ServiceBase, IService {

    public class AssetTypeGroups
    {
        public int[] clothing { get; set; }
        public int[] accessories { get; set; }
        public int[] avataranimations { get; set; }
        public int[] bodyparts { get; set; }
        public int[] packages { get; set; }
        public int[] all { get; set; }
    }
    
    public AssetTypeGroups recentAssetTypes = new AssetTypeGroups
    {
        clothing = new[]{2, 11, 12},
        accessories = new[]{8,41,42,43,44,45,46,47},
        avataranimations = new[]{48,50,51,52,53,54,55,61,18},
        bodyparts = new[]{17,27,28,29,30,31},
        packages = new[]{31},
        all = new[]{2, 11, 12, 8, 41, 42, 43, 44, 45, 46, 47, 48, 50, 51, 52, 53, 54, 55, 61, 18, 17, 27, 28, 29, 30, 31, 31, 19},
    };
    
    public async Task<IEnumerable<long>> GetWornAssets(long userId)
    {
        // useless inner join is intentional:
        // it's so that we filter out items the user no longer owns.
        return (await db.QueryAsync<AssetId>(
            "SELECT distinct(ua.asset_id) as assetId FROM user_avatar_asset av INNER JOIN user_asset ua ON ua.user_id = av.user_id AND ua.asset_id = av.asset_id WHERE av.user_id = :user_id", new
            {
                user_id = userId,
            })).Select(c => c.assetId);
    }

    public async Task<bool> IsUserAvatar18Plus(long userId)
    {
        var result = await db.QuerySingleOrDefaultAsync<Dto.Total>
        ("SELECT count(*) AS total FROM user_avatar_asset INNER JOIN asset ON asset.id = user_avatar_asset.asset_id WHERE asset.is_18_plus AND user_avatar_asset.user_id = :user_id",
            new
            {
                user_id = userId,
            });
        return result.total != 0;
    }

    public async Task<IEnumerable<long>> GetRecentItems(long userId)
    {
        var result =
            await db.QueryAsync(
                "SELECT distinct asset_id, max(id) FROM user_asset WHERE user_id = :user_id GROUP BY asset_id ORDER BY max(id) DESC, asset_id LIMIT 50",
                new
                {
                    user_id = userId,
                });
        return result.Select(c => (long) c.asset_id);
    }
    public async Task<IEnumerable<long>> GetRecentAvatarItems(long userId, int[] assetTypes)
    {
        var result =
            await db.QueryAsync(
                "SELECT asset_id FROM user_asset ua INNER JOIN asset a ON a.id = ua.asset_id WHERE user_id = :user_id AND a.asset_type = ANY(:types) ORDER BY ua.updated_at DESC LIMIT 25",
                new
                {
                    user_id = userId,
                    types = assetTypes.Select(i => (short)i).ToArray(),
                });
        return result.Select(c => (long) c.asset_id);
    }
    public async Task<AvatarType> GetAvatarType(long userId)
    {
        var result = await db.QuerySingleOrDefaultAsync<AvatarType>(
            "SELECT avatar_type as avatarType FROM user_avatar WHERE user_id = :user_id",
            new
            {
                user_id = userId,
            });

        return result;
    }
    
    public async Task<BodyScales> GetAvatarScales(long userId)
    {
        var avatarScales = await db.QuerySingleOrDefaultAsync<BodyScales>(
            @"SELECT 
                scale_height as height,
                scale_width as width,
                scale_head as head,
                scale_depth as depth,
                scale_proportion as proportion,
                scale_body_type as bodyType
                FROM user_avatar WHERE user_id = :user_id",
            new
            {
                user_id = userId,
            });

        return avatarScales;
    }

    public async Task UpdateRigType(AvatarType type, long userId)
    {
        await db.ExecuteAsync(
            "UPDATE user_avatar SET avatar_type = :avatar_type WHERE user_id = :user_id",
            new
            {
                avatar_type = (int)type,
                user_id = userId,
            });
    }
    
    // DOES NOT CHECK FOR CORRECT SCALES, do it yourself below lal
    public async Task UpdateBodyScales(BodyScales scales, long userId)
    {
        await db.ExecuteAsync(
            @"UPDATE user_avatar SET 
                       scale_height = :height,
                       scale_width = :width,
                       scale_head = :head,
                       scale_depth = :depth,
                       scale_proportion = :proportion,
                       scale_body_type = :bodyType
                  WHERE user_id = :userId",
            new
            {
                userId,
                height = Math.Round(scales.height, 2),
                width = Math.Round(scales.width, 2),
                head = Math.Round(scales.head, 2),
                depth = Math.Round(scales.depth, 2),
                proportion = Math.Round(scales.proportion, 2),
                bodyType = Math.Round(scales.bodyType, 2),
            });
    }
    
    // LIMITS:
    /*
     * HEIGHT: 90-105%
     * WIDTH: 70-100%
     * HEAD: 90-100%
     * DEPTH: same as Width, usually merged actually but is different on API
     * PROPORTION: 0-100%
     * BODYTYPE: 0-100%
     */
    public bool AreScalesValid(BodyScales scales) {

        // i feel like there's a better way to do this but idk lal
        if (scales.height > 1.05 || scales.height < 0.9) return false;
        if (scales.width > 1 || scales.width < 0.7) return false;
        if (scales.head > 1 || scales.head < 0.9) return false;
        if (scales.depth > 1 || scales.depth < 0.7) return false;
        if (scales.proportion > 1 || scales.proportion < 0) return false;
        if (scales.bodyType > 1 || scales.bodyType < 0) return false;

        return true;
    }

    public async Task<AvatarWithColors> GetAvatar(long userId)
    {
        var existingAvatar = await db.QuerySingleOrDefaultAsync<DatabaseAvatarWithImages>(
            "SELECT * FROM user_avatar WHERE user_id = :user_id",
            new
            {
                user_id = userId,
            });
        if (existingAvatar == null)
            throw new RecordNotFoundException();
        return new AvatarWithColors()
        {
            headColorId = existingAvatar.head_color_id,
            torsoColorId = existingAvatar.torso_color_id,
            rightArmColorId = existingAvatar.right_arm_color_id,
            leftArmColorId = existingAvatar.left_arm_color_id,
            rightLegColorId = existingAvatar.right_leg_color_id,
            leftLegColorId = existingAvatar.left_leg_color_id,
            avatarType = existingAvatar.avatar_type,
            thumbnailUrl = existingAvatar.thumbnail_url,
            thumbnail3DUrl = existingAvatar.thumbnail_3d_url,
            headshotUrl = existingAvatar.headshot_thumbnail_url,
            scales = new BodyScales
            {
                width = existingAvatar.scale_width,
                height = existingAvatar.scale_height,
                head = existingAvatar.scale_head,
                depth = existingAvatar.scale_depth,
                proportion = existingAvatar.scale_proportion,
                bodyType = existingAvatar.scale_body_type
            }
        };
    }

    public async Task<ColorEntry> GetAvatarColors(long userId)
    {
        var existingAvatar = await db.QuerySingleOrDefaultAsync<DatabaseAvatar>(
            "SELECT head_color_id, torso_color_id, left_arm_color_id,right_arm_color_id,left_leg_color_id,right_leg_color_id FROM user_avatar WHERE user_id = :user_id",
            new
            {
                user_id = userId,
            });
        return new ColorEntry()
        {
            headColorId = existingAvatar.head_color_id,
            torsoColorId = existingAvatar.torso_color_id,
            rightArmColorId = existingAvatar.right_arm_color_id,
            leftArmColorId = existingAvatar.left_arm_color_id,
            rightLegColorId = existingAvatar.right_leg_color_id,
            leftLegColorId = existingAvatar.left_leg_color_id,
        };
    }

    private readonly Models.Assets.Type[] _wearableAssetTypes = new[]
    {

        Type.Shirt,
        Type.Pants,
        Type.TeeShirt,
        //accesories
        Type.Face,
        Type.Hat,
        Type.FrontAccessory,
        Type.BackAccessory,
        Type.WaistAccessory,
        Type.HairAccessory,
        Type.NeckAccessory,
        Type.ShoulderAccessory,
        Type.FaceAccessory,

        //body
        Type.LeftArm,
        Type.RightArm,
        Type.LeftLeg,
        Type.RightLeg,
        Type.Torso,
        Type.Head,

        //anims
        Type.ClimbAnimation,
        Type.DeathAnimation,
        Type.FallAnimation,
        Type.IdleAnimation,
        Type.JumpAnimation,
        Type.RunAnimation,
        Type.SwimAnimation,
        Type.WalkAnimation,
        Type.PoseAnimation,
        Type.EmoteAnimation,
        //gears
        Type.Gear,
    };

    /// <summary>
    /// Filter the dirtyAssetIds. This will remove moderated/pending items, items the user doesn't own, invalid items, etc.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="dirtyAssetIds"></param>
    /// <returns></returns>
    public async Task<IEnumerable<long>> FilterAssetsForRender(long userId, IEnumerable<long> dirtyAssetIds)
    {
        var assetIds = dirtyAssetIds.ToList();
        if (assetIds.Count != 0)
        {
            using var assets = ServiceProvider.GetOrCreate<AssetsService>();
            // Get the moderation status for each item
            var moderationStatus = (await db.QueryAsync<AssetModerationEntry>(
                "SELECT moderation_status as moderationStatus, id as assetId, asset_type as assetType FROM asset WHERE id = ANY(:ids)", new
                {
                    ids = assetIds,
                })).ToList();
            // Duplicate the list so we can mutate it
            var safeModList = moderationStatus.ToList();
            // Add package contents, if required
            foreach (var item in moderationStatus)
            {
                if (item.assetType == Type.Package)
                {
                    var packageAssetIds = await assets.GetPackageAssets(item.assetId);
                    var otherModStatus = (await db.QueryAsync<AssetModerationEntry>(
                        "SELECT moderation_status as moderationStatus, id as assetId, asset_type as assetType FROM asset WHERE id = ANY(:ids)", new
                        {
                            ids = packageAssetIds.ToList(),
                        })).ToList();
                    foreach (var nestedAsset in otherModStatus)
                    {
                        safeModList.Add(nestedAsset);
                        assetIds.Add(nestedAsset.assetId);
                    }
                }
            }
            // Filter items by moderation status - we only want to render ReviewApproved
            assetIds = assetIds.Where(c =>
            {
                var hasEntry = safeModList.Find(v => v.assetId == c);
                if (hasEntry == null) return false;
                if (!_wearableAssetTypes.Contains(hasEntry.assetType)) return false;
                if (hasEntry.moderationStatus != ModerationStatus.ReviewApproved) return false;
                return true;
            }).ToList();
            // Finally, confirm user actually owns each assetId
            // goodAssetIds is a list of assets the user owns
            var goodAssetIds = new List<long>();
            foreach (var id in assetIds)
            {
                var ownResult = await db.QuerySingleOrDefaultAsync<UserAssetEntry>(
                    "SELECT asset_id as assetId, user_id as userId from user_asset WHERE user_id = :user_id AND asset_id = :asset_id LIMIT 1",
                    new
                    {
                        user_id = userId,
                        asset_id = id,
                    });
                if (ownResult != null)
                {
                    goodAssetIds.Add(id);
                }
            }

            assetIds = goodAssetIds;
        }

        return assetIds;
    }

    public string GetAvatarHash(ColorEntry colors, IEnumerable<long> assetVersionIds, BodyScales scales, AvatarType rigType)
    {
        var assets = assetVersionIds.Distinct().ToList();
        assets.Sort((a, b) => a > b ? 1 : a == b ? 0 : -1);
        var str =
            $@"avatar-hash-1.5:
                {string.Join(",", assets)}:
                {colors.headColorId},
                {colors.torsoColorId},
                {colors.leftArmColorId},
                {colors.rightArmColorId},
                {colors.leftLegColorId},
                {colors.rightLegColorId},
                {scales.height},
                {scales.width},
                {scales.depth},
                {scales.proportion},
                {scales.head},
                {scales.bodyType},
                {(int)rigType},
            ";
        var hasher = SHA256.Create();
        var bits = hasher.ComputeHash(Encoding.UTF8.GetBytes(str));
        return Convert.ToHexString(bits).ToLower();
    }

    private async Task<IEnumerable<long>> MultiGetAssetVersionsFromAssetIds(IEnumerable<long> assetIds)
    {
        // todo: make this more efficient :(
        var ids = new List<long>();
        using var assets = ServiceProvider.GetOrCreate<AssetsService>(this);
        foreach (var id in assetIds.Distinct())
        {
            var latest = await assets.GetLatestAssetVersion(id);
            ids.Add(latest.assetVersionId);
        }
        return ids.Distinct();
    }

    /// <summary>
    /// Update the userId's avatar. Returns a hash. This does not render or validate anything.
    /// </summary>
    public async Task<string> UpdateUserAvatar(long userId, ColorEntry colors, IEnumerable<long> assetIds,
        BodyScales scales, AvatarType rigType)
    {
        var idsList = assetIds.ToList();
        return await InTransaction(async (trx) =>
        {
            await UpdateAsync("user_avatar", "user_id", userId, new
            {
                head_color_id = colors.headColorId,
                torso_color_id = colors.torsoColorId,
                right_arm_color_id = colors.rightArmColorId,
                left_arm_color_id = colors.leftArmColorId,
                right_leg_color_id = colors.rightLegColorId,
                left_leg_color_id = colors.leftLegColorId,
                scale_height = scales.height,
                scale_width = scales.width,
                scale_head = scales.head,
                scale_depth = scales.depth,
                scale_proportion = scales.proportion,
                scale_body_type = scales.bodyType,
                avatar_type = (int)rigType,
            });
            await db.ExecuteAsync("DELETE FROM user_avatar_asset WHERE user_id = :user_id", new
            {
                user_id = userId,
            });
            foreach (var item in idsList)
            {
                await db.ExecuteAsync("INSERT INTO user_avatar_asset (user_id, asset_id) VALUES (:user_id, :asset_id)",
                    new
                    {
                        user_id = userId,
                        asset_id = item,
                    });
            }

            var assetVersions = await MultiGetAssetVersionsFromAssetIds(idsList);
            return GetAvatarHash(colors, assetVersions, scales, rigType);
        });
    }

    public async Task UpdateUserAvatarImages(long userId, string? headshotImage, string? thumbnailImage, string? thumbnail3DImage)
    {
        await db.ExecuteAsync(
            "UPDATE user_avatar SET thumbnail_url = :thumbnail_url, headshot_thumbnail_url = :headshot_url, thumbnail_3d_url = :thumbnail_3d_url WHERE user_id = :user_id",
            new
            {
                user_id = userId,
                thumbnail_url = thumbnailImage,
                thumbnail_3d_url = thumbnail3DImage,
                headshot_url = headshotImage,
            });
    }

    public async Task<IEnumerable<OutfitEntry>> GetUserOutfits(long userId, int limit, int offset)
    {
        return await db.QueryAsync<OutfitEntry>(
            "SELECT id, name, created_at as created FROM user_outfit WHERE user_id = :user_id ORDER BY id DESC LIMIT :limit OFFSET :offset",
            new
            {
                user_id = userId,
                limit,
                offset,
            });
    }

    public async Task<OutfitExtendedDetails> GetOutfitById(long outfitId)
    {
        var result = await db.QuerySingleOrDefaultAsync<OutfitAvatar>(
            @"SELECT 
                    name,
                    head_color_id as headColorId,
                    torso_color_id as torsoColorId,
                    left_arm_color_id as leftArmColorId,
                    right_arm_color_id as rightArmColorId,
                    left_leg_color_id as leftLegColorId,
                    right_leg_color_id as rightLegColorId,
                    user_id as userId,
                    scale_height as height,
                    scale_width as width,
                    scale_head as head,
                    scale_depth as depth,
                    scale_proportion as proportion,
                    scale_body_type as bodyType,
                    avatar_type as avatarType
                  FROM user_outfit WHERE id = :id",
            new
            {
                id = outfitId,
            });
        var assets =
            (await db.QueryAsync<AssetId>(
                "SELECT asset_id as assetId FROM user_outfit_asset WHERE outfit_id = :outfit_id",
                new {outfit_id = outfitId})).Select(c => c.assetId);

        return new OutfitExtendedDetails()
        {
            assetIds = assets,
            details = result,
        };
    }

    public async Task CreateOutfit(long userId, string name, string? thumbnailUrl, string? headshotUrl,
        OutfitExtendedDetails outfitDetails)
    {
        var existingOutfitCount = await db.QuerySingleOrDefaultAsync<Total>(
            "SELECT COUNT(*) as total FROM user_outfit WHERE user_id = :user_id", new
            {
                user_id = userId,
            });
        if (existingOutfitCount.total >= 100)
            throw new TooManyOutfitsException();
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new OutfitNameTooShortException();
        if (name.Length > 25)
            throw new OutfitNameTooLongException();
        // image check
        if (string.IsNullOrWhiteSpace(thumbnailUrl) || string.IsNullOrWhiteSpace(headshotUrl))
            throw new NoImageUrlException();

        await InTransaction(async (trx) =>
        {
            var id = await InsertAsync("user_outfit", new
            {
                name = name,
                user_id = userId,
                // colors
                head_color_id = outfitDetails.details.headColorId,
                torso_color_id = outfitDetails.details.torsoColorId,
                left_arm_color_id = outfitDetails.details.leftArmColorId,
                right_arm_color_id = outfitDetails.details.rightArmColorId,
                left_leg_color_id = outfitDetails.details.leftLegColorId,
                right_leg_color_id = outfitDetails.details.rightLegColorId,
                // type
                avatar_type = outfitDetails.details.avatarType,
                // scales
                scale_height = outfitDetails.details.height,
                scale_width = outfitDetails.details.width,
                scale_head = outfitDetails.details.head,
                scale_depth = outfitDetails.details.depth,
                scale_proportion = outfitDetails.details.proportion,
                scale_body_type = outfitDetails.details.bodyType,
                // images
                headshot_thumbnail_url = headshotUrl,
                thumbnail_url = thumbnailUrl,
            });
            foreach (var assetId in outfitDetails.assetIds)
            {
                await InsertAsync("user_outfit_asset", "outfit_id", new
                {
                    outfit_id = id,
                    asset_id = assetId,
                });
            }

            return 0;
        });
    }

    public async Task RenameOutfit(long outfitId, string name) {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new OutfitNameTooShortException();
        if (name.Length > 25)
            throw new OutfitNameTooLongException();
        await UpdateAsync("user_outfit", outfitId, new {
            name
        });
    }
    
    public async Task UpdateOutfit(long outfitId, string? name, string? thumbnailUrl, string? headshotUrl,
        OutfitExtendedDetails outfitDetails)
    {
        if (name != null) {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
                throw new OutfitNameTooShortException();
            if (name.Length > 25)
                throw new OutfitNameTooLongException();
        }
        // image check
        if (string.IsNullOrWhiteSpace(thumbnailUrl) || string.IsNullOrWhiteSpace(headshotUrl))
            throw new NoImageUrlException();
        await InTransaction(async (trx) =>
        {
            await UpdateAsync("user_outfit", outfitId, new
            {
                // colors
                head_color_id = outfitDetails.details.headColorId,
                torso_color_id = outfitDetails.details.torsoColorId,
                left_arm_color_id = outfitDetails.details.leftArmColorId,
                right_arm_color_id = outfitDetails.details.rightArmColorId,
                left_leg_color_id = outfitDetails.details.leftLegColorId,
                right_leg_color_id = outfitDetails.details.rightLegColorId,
                // type
                avatar_type = outfitDetails.details.avatarType,
                // scales
                scale_height = outfitDetails.details.height,
                scale_width = outfitDetails.details.width,
                scale_head = outfitDetails.details.head,
                scale_depth = outfitDetails.details.depth,
                scale_proportion = outfitDetails.details.proportion,
                scale_body_type = outfitDetails.details.bodyType,
                // images
                headshot_thumbnail_url = headshotUrl,
                thumbnail_url = thumbnailUrl,
            });
            if (name != null) {
                // TODO: there's gotta be a better way to do this but idk hjow
                await UpdateAsync("user_outfit", outfitId, new { name });
            }
            await db.ExecuteAsync("DELETE FROM user_outfit_asset WHERE outfit_id = :id", new {id = outfitId});
            foreach (var assetId in outfitDetails.assetIds)
            {
                await InsertAsync("user_outfit_asset", "outfit_id", new
                {
                    outfit_id = outfitId,
                    asset_id = assetId,
                });
            }

            return 0;
        });
    }

    public async Task DeleteOutfit(long outfitId)
    {
        await InTransaction(async (t) =>
        {
            await db.ExecuteAsync("DELETE FROM user_outfit WHERE id = :id", new
            {
                id = outfitId,
            });
            await db.ExecuteAsync("DELETE FROM user_outfit_asset WHERE outfit_id = :outfit_id", new
            {
                outfit_id = outfitId,
            });
            return 0;
        });
    }

    private async Task<bool> ConfirmAssetSelectionIsOkForRender(IEnumerable<long> unknownAssetIds)
    {
        var assets = new AssetsService();
        var assetIds = unknownAssetIds.ToList();
        if (assetIds.Count == 0) return true;
        var details = await assets.MultiGetInfoById(assetIds);

        // vars
        var gear = 0;
        var face = 0;
        var shirt = 0;
        var pants = 0;
        var tShirt = 0;
        var accessories = 0;
        var leftArm = 0;
        var rightArm = 0;
        var leftLeg = 0;
        var rightLeg = 0;
        var torso = 0;
        var head = 0;
        var animations = 0;
        var emotes = 0;
        var climbAnimation = 0;
        var fallAnimation = 0;
        var idleAnimation = 0;
        var walkAnimation = 0;
        var runAnimation = 0;
        var jumpAnimation = 0;
        var poseAnimation = 0;
        var swimAnimation = 0;

        foreach (var item in details)
        {
            switch (item.assetType)
            {
                case Type.TeeShirt:
                    tShirt++;
                    break;
                case Type.Shirt:
                    shirt++;
                    break;
                case Type.Pants:
                    pants++;
                    break;
                case Type.Animation:
                    animations++;
                    break;
                case Type.Gear:
                    gear++;
                    break;
                case Type.Face:
                    face++;
                    break;
                case Type.Hat:
                case Type.FrontAccessory:
                case Type.BackAccessory:
                case Type.HairAccessory:
                case Type.NeckAccessory:
                case Type.ShoulderAccessory:
                case Type.WaistAccessory:
                case Type.FaceAccessory:
                    accessories++;
                    break;
                case Type.Head:
                    head++;
                    break;
                case Type.Torso:
                    torso++;
                    break;
                case Type.LeftArm:
                    leftArm++;
                    break;
                case Type.RightArm:
                    rightArm++;
                    break;
                case Type.LeftLeg:
                    leftLeg++;
                    break;
                case Type.RightLeg:
                    rightLeg++;
                    break;
                case Type.FallAnimation:
                    fallAnimation++;
                    break;
                case Type.ClimbAnimation:
                    climbAnimation++;
                    break;
                case Type.IdleAnimation:
                    idleAnimation++;
                    break;
                case Type.WalkAnimation:
                    walkAnimation++;
                    break;
                case Type.RunAnimation:
                    runAnimation++;
                    break;
                case Type.JumpAnimation:
                    jumpAnimation++;
                    break;
                case Type.PoseAnimation:
                    poseAnimation++;
                    break;
                case Type.SwimAnimation:
                    swimAnimation++;
                    break;
                case Type.EmoteAnimation:
                    emotes++;
                    break;
                default:
                    throw new Exception("Unexpected asset type: " + item.assetType);
            }
        }

        if (gear > 1) return false;
        if (tShirt > 1 || shirt > 1 || pants > 1) return false;
        if (face > 1) return false;
        // Orginal is 7
        if (accessories > 15) return false;
        if (leftArm > 1 || rightArm > 1 || leftLeg > 1 || rightLeg > 1 || torso > 1 || head > 1) return false;
        if (emotes > 8) return false;
        if (idleAnimation > 1 || 
            walkAnimation > 1 || 
            runAnimation > 1 || 
            jumpAnimation > 1 || 
            poseAnimation > 1 || 
            swimAnimation > 1 || 
            fallAnimation > 1) 
        {
            return false;
        }

        return true;
    }

    private bool IsColorValid(int color)
    {
        var allColors = Roblox.Models.Avatar.AvatarMetadata.GetColors();
        foreach (var item in allColors)
        {
            if (item.brickColorId == color)
            {
                return true;
            }
        }

        return false;
    }

    public async Task UpdateLastUpdated(long userId, long assetId) {
        await db.QueryAsync("UPDATE user_asset SET updated_at = now() WHERE user_id = :userId AND asset_id = :assetId", new
        {
            userId,
            assetId,
        });
    }

    public bool AreColorsOk(ColorEntry colors)
    {
        if (!IsColorValid(colors.headColorId)) return false;
        if (!IsColorValid(colors.torsoColorId)) return false;
        if (!IsColorValid(colors.leftArmColorId)) return false;
        if (!IsColorValid(colors.rightArmColorId)) return false;
        if (!IsColorValid(colors.leftLegColorId)) return false;
        if (!IsColorValid(colors.rightLegColorId)) return false;
        return true;
    }

    public string GetAvatarRedLockKey(long userId)
    {
        return $"update avatar web v1 {userId}";
    }

    public async Task Update3DRenderModified(long userId, string avatarHash)
    {
        var thumbnail3DUrl = $"/images/thumbnails/{avatarHash}_thumbnail3d.json";
        try {
            var thumbnail3DJson = await File.ReadAllTextAsync(Configuration.PublicDirectory + thumbnail3DUrl);
            var thumbJson = JsonSerializer.Deserialize<Thumbnail3DRendered>(thumbnail3DJson);
            if (thumbJson is null)
                throw new Exception("Renderer returned incorrect 3D thumbnail.");

            var objPath = Configuration.ThumbnailsDirectory + thumbJson.obj.Replace("/images/thumbnails/", "");
            if (File.Exists(objPath)) {
                File.SetLastWriteTime(objPath, DateTime.Now);
            }

            var mtlPath = Configuration.ThumbnailsDirectory + thumbJson.mtl.Replace("/images/thumbnails/", "");
            if (File.Exists(mtlPath)) {
                File.SetLastWriteTime(mtlPath, DateTime.Now);
            }

            foreach (var filePath2 in thumbJson.textures) {
                var filePath = Configuration.ThumbnailsDirectory + filePath2.Replace("/images/thumbnails/", "");
                if (File.Exists(filePath)) {
                    File.SetLastWriteTime(filePath, DateTime.Now);
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine($"[AvatarService] Couldn't set last updated for 3D Render for AvatarHash {avatarHash}. Error: {e.Message}");
            if (e.Message.StartsWith("Could not find file")) {
                Console.WriteLine("[AvatarService] Redrawing avatar due to missing 3D render files...");
                await RedrawAvatar(userId);
            }
        }
    }

    public async Task RedrawAvatar(long userId, IEnumerable<long>? newAssetIds = null, ColorEntry? colors = null,
        AvatarType? avatarType = null, bool forceRedraw = false, bool ignoreLock = false, 
        bool skipRender = false, BodyScales? scales = null)
    {
        // required services
        using var assets = ServiceProvider.GetOrCreate<AssetsService>();
        
        await using var redLock =
            await Cache.redLock.CreateLockAsync(GetAvatarRedLockKey(userId), TimeSpan.FromSeconds(5));
        if (!redLock.IsAcquired && !ignoreLock) throw new LockNotAcquiredException();

        var assetIds = newAssetIds?.ToList();
        
        // If list provided is null, then the caller wants us to grab the items ourselves

        avatarType ??= await GetAvatarType(userId); 
        scales ??= await GetAvatarScales(userId);
        colors ??= await GetAvatarColors(userId);
        assetIds ??= (await GetWornAssets(userId)).ToList();
        
        // TODO: do we check if avatar type is valid? probably not but either way that should prob be rewritten
        //  (the way we get AvatarType from int
        if (!AreColorsOk(colors))
            throw new RobloxException(400, 0, "Colors are invalid");
        if (!AreScalesValid(scales) && userId is not (68 or 3))
            throw new RobloxException(400, 0, "Scales are invalid");

        if (assetIds.Count != 0)
        {
            assetIds = (await FilterAssetsForRender(userId, assetIds)).ToList();
        }

        var assetsOk = await ConfirmAssetSelectionIsOkForRender(assetIds);
        if (!assetsOk)
            throw new RobloxException(400, 0, "One or more assets are invalid");
        // Now, update the avatar. This returns a hash
        var avatarHash = await UpdateUserAvatar(userId, colors, assetIds, scales, avatarType.Value);

        if (skipRender) return;
        // We don't wanna waste time rendering if we don't have to

        // Get our image urls
        var thumbnailUrl = $"/images/thumbnails/{avatarHash}_thumbnail.png";
        var thumbnail3DUrl = $"/images/thumbnails/{avatarHash}_thumbnail3d.json";
        var headshotUrl = $"/images/thumbnails/{avatarHash}_headshot.png";
        if (!forceRedraw)
        {
            // Check if the hash exists already - If they do, we can skip rendering!
            if (File.Exists(Configuration.PublicDirectory + thumbnailUrl) &&
                File.Exists(Configuration.PublicDirectory + thumbnail3DUrl) &&
                File.Exists(Configuration.PublicDirectory + headshotUrl))
            {
                // Since both files exist, we can just update the URL and exit
                await UpdateUserAvatarImages(userId, headshotUrl, thumbnailUrl, thumbnail3DUrl);
                // We must mark the 3D render as used (so it doesn't get deleted on 3D render cleanup)
                await Update3DRenderModified(userId, avatarHash);
                return;
            }
        }
        // We have to call render library now.
        // Set image urls to null:
        await UpdateUserAvatarImages(userId, null, null, null);

        // Sane timeout of 2 minutes. If a render takes longer than this, something's probably broken
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(TimeSpan.FromMinutes(2));
        // Make both requests at once
        var tasks = new List<Task<string>>()
        {
            RenderingHandler.RequestHeadshotThumbnail(userId),
            RenderingHandler.RequestPlayerThumbnail(userId),
        };
        var result = await Task.WhenAll(tasks);
        var headshotResult = result[0];
        var thumbnailResult = result[1];

        try
        {
            string headshotFileName = $"{avatarHash}_headshot.png";

            using (var headshotStream = await RenderingHandler.ResizeImage<MemoryStream, string>(headshotResult, 150, 150))
            using (var headshotFile = new FileStream(
                Path.Combine(Configuration.ThumbnailsDirectory, headshotFileName),
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                headshotStream.Position = 0;
                await headshotStream.CopyToAsync(headshotFile);
            }

            string thumbnailFileName = $"{avatarHash}_thumbnail.png";

            using (var thumbnailStream = await RenderingHandler.ResizeImage<MemoryStream, string>(thumbnailResult, 352, 352))
            using (var thumbnailFile = new FileStream(
                Path.Combine(Configuration.ThumbnailsDirectory, thumbnailFileName),
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                thumbnailStream.Position = 0;
                await thumbnailStream.CopyToAsync(thumbnailFile);
            }
        }
        catch (Exception ex)
        {
            Writer.Info(LogGroup.AvatarService, "Failed to save headshot or thumbnail for user {0}: {1}", userId, ex.Message);
            await UpdateUserAvatarImages(userId, headshotUrl, thumbnailUrl, null);
            return;
        }

        // Update the avatar thumbnail, excluding 3D
        await UpdateUserAvatarImages(userId, headshotUrl, thumbnailUrl, null);

        var thumbnail3DResult = await RenderingHandler.RequestPlayerThumbnail3D(userId);
        // Now thumbnail 3D, these have a unique format in JSON
        try
        {
            // TODO: consider resizing these 3D renders — they may be too large
            var thumbJson = JsonSerializer.Deserialize<Thumbnail3DRender>(thumbnail3DResult);
            if (thumbJson is null)
                throw new Exception("Renderer returned incorrect 3D thumbnail.");

            string? obj = null;
            string? mtl = null;
            var textures = new List<string>();
            
            using SHA256 hasher = SHA256.Create();

            if (thumbJson.files.TryGetValue("scene.obj", out var sceneObj))
            {
                byte[] objData = Convert.FromBase64String(sceneObj.content);
                string objHash = Convert.ToHexString(hasher.ComputeHash(objData)).ToLower();
                string objFileName = objHash;
                string objDiskPath = Path.Combine(Configuration.ThumbnailsDirectory, "3d", objFileName);
                string objUrlPath = $"/images/thumbnails/3d/{objFileName}";

                obj = objUrlPath;

                if (!File.Exists(objDiskPath))
                {
                    await File.WriteAllBytesAsync(objDiskPath, objData);
                }
            }

            if (thumbJson.files.TryGetValue("scene.mtl", out var sceneMtl))
            {
                byte[] mtlData = Convert.FromBase64String(sceneMtl.content);
                string mtlHash = Convert.ToHexString(hasher.ComputeHash(mtlData)).ToLower();
                string mtlFileName = mtlHash;
                string mtlDiskPath = Path.Combine(Configuration.ThumbnailsDirectory, "3d", mtlFileName);
                string mtlUrlPath = $"/images/thumbnails/3d/{mtlFileName}";

                mtl = mtlUrlPath;

                if (!File.Exists(mtlDiskPath))
                {
                    await File.WriteAllBytesAsync(mtlDiskPath, mtlData);
                }
            }

            foreach (var (fileName, fileValue) in thumbJson.files)
            {
                if (!fileName.Contains("tex.png", StringComparison.OrdinalIgnoreCase))
                    continue;

                byte[] textureData = Convert.FromBase64String(fileValue.content);
                string textureHash = Convert.ToHexString(hasher.ComputeHash(textureData)).ToLower();
                string baseName = fileName.Replace(".png", "", StringComparison.OrdinalIgnoreCase);
                string textureFileName = $"{textureHash}_tex_{baseName}";
                string textureDiskPath = Path.Combine(Configuration.ThumbnailsDirectory, "3d", textureFileName);
                string textureUrlPath = $"/images/thumbnails/3d/{textureFileName}";

                if (!File.Exists(textureDiskPath))
                {
                    await File.WriteAllBytesAsync(textureDiskPath, textureData);
                }

                textures.Add(textureUrlPath);
            }

            var thumbnail3DJson = new
            {
                userId,
                thumbJson.camera,
                aabb = new
                {
                    thumbJson.AABB.min,
                    thumbJson.AABB.max
                },
                mtl,
                obj,
                textures = textures.Count > 0 ? textures.ToArray() : null
            };

            string jsonOutputPath = Path.Combine(Configuration.ThumbnailsDirectory, $"{avatarHash}_thumbnail3d.json");
            string jsonContent = JsonSerializer.Serialize(thumbnail3DJson);
            await File.WriteAllBytesAsync(jsonOutputPath, Encoding.UTF8.GetBytes(jsonContent));

            // Finally, update the avatar thumbnail (including 3D version)
            await UpdateUserAvatarImages(userId, headshotUrl, thumbnailUrl, thumbnail3DUrl);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[RewrittenRCC]: Failed to save 3D thumbnail: {e}");
        }

    }

    // public async Task TryAsset(long userId, long assetId)
    // {
    // }

    public bool IsThreadSafe()
    {
        return true;
    }

    public bool IsReusable()
    {
        return false;
    }

    private static Timer _timer;

    public static void StartTimerClear3D()
    {
        _timer = new Timer(_ =>
        {
            // Clear 3D folder of any 3D asset that hasn't been used in 5+ days
            Writer.Info(LogGroup.ClearThumbnail3DFolder, "Clearing 3D thumbnails that are older than 5 days...");
            var thumbnail3DFolder = $"{Configuration.ThumbnailsDirectory}3d";
            if (!Directory.Exists(thumbnail3DFolder))
            {
                Writer.Info(LogGroup.ClearThumbnail3DFolder, "3D thumbnail folder does not exist, returning...");
                return;
            };

            foreach (var file in Directory.GetFiles(thumbnail3DFolder))
            {
                var lastModified = File.GetLastWriteTime(file);
                var fiveDaysAgo = DateTime.Now.Subtract(TimeSpan.FromDays(5));
                if (!(lastModified < fiveDaysAgo)) continue;
                
                // Delete file.
                Writer.Info(LogGroup.ClearThumbnail3DFolder, $"Deleting 3D thumbnail {Path.GetFileName(file)}, last modified is more than 5 days ago. Last modified: {lastModified}");
                File.Delete(file);
            }
            Writer.Info(LogGroup.ClearThumbnail3DFolder, "Successfully cleared 3D thumbnails that were older than 5 days.");
        }, null, TimeSpan.FromSeconds(3), TimeSpan.FromHours(3));
    }
}