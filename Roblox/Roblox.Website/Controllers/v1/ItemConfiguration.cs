using System.Collections;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Assets;
using Roblox.Exceptions;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Services.Exceptions;
using Roblox.Website.WebsiteModels.Catalog;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/itemconfiguration/v1")]
public class ItemConfigurationV1 : ControllerBase
{
    [HttpGet("metadata")]
    public dynamic GetMetadata()
    {
        return new
        {
            allowedAssetTypeForUpload = (object?)null,
            allowedAssetTypesForRelease = (object?)null,
            allowedAssetTypesForFree = new List<string>(){"Plugin"},
            allowedAssetTypesForSaleAvailabilityLocations = new List<string>()
            {
                "Hat",
                "HairAccessory",
                "FaceAccessory",
                "NeckAccessory",
                "ShoulderAccessory",
                "FrontAccessory",
                "BackAccessory",
                "WaistAccessory",
            },
        };
    }

    [HttpPostBypass("/v1/creations/get-asset-details")]
    [HttpPost("creations/get-asset-details")]
    public async Task<dynamic> MultiGetAssetDetails([Required, FromBody] MultiGetAssetDetailsRequest request)
    {
        return await services.assets.MultiGetAssetDeveloperDetails(request.assetIds);
    }
    
    [HttpGet("assets/{assetId:long}/get-selling-fee")]
    [HttpGetBypass("/v1/assets/{assetId:long}/get-selling-fee")]
    public dynamic GetSellingFee()
    {
        return new
        {
            price = 0,
        };
    }

    // [HttpPost("assets/{assetId:long}/update")]
    // public async Task UpdateAssetNoOp(long assetId)
    // {
    //     return;
    // }

    private readonly Models.Assets.Type[] sellableAssetTypes = {
        Type.Shirt,
        Type.Pants,
        Type.TeeShirt,
        Type.GamePass
    };

    private readonly Type[] freeAssetTypes =
    {
        Type.Animation,
        Type.Image,
        Type.Decal,
        Type.Image, // why are there 2 here
        Type.Mesh,
        Type.MeshPart,
        Type.Model,
        Type.SolidModel,
    };

    [HttpPostBypass("/v1/assets/{assetId:long}/update-price")]
    [HttpPost("assets/{assetId:long}/update-price")]
    public async Task UpdateAssetPrice(long assetId, [Required,FromBody] UpdateAssetPriceRequest request)
    {
        
        if (request.priceInRobux != 0 && (request.priceInRobux is > 1000000 or < 2))
            throw new BadRequestException(0, "Bad robux price");
        

        if (request.priceInTickets is 0)
        {
            request.priceInTickets = null;
        }

        if (request.priceInTickets is > 1000000 or < 2)
            throw new BadRequestException(0, "Bad ticket price");
        
        
        var details = await services.assets.GetAssetCatalogInfo(assetId);
        if (!sellableAssetTypes.Contains(details.assetType))
        {
            if (!freeAssetTypes.Contains(details.assetType))
            {
                throw new BadRequestException(0, "Bad asset type");
            }
            if (request.priceInRobux != null)
                request.priceInRobux = 0;
            request.priceInTickets = null;
        }

        // Both null mean not for sale
        var isForSale = request.priceInTickets != null || request.priceInRobux != null;

        await services.assets.ValidatePermissions(assetId, safeUserSession.userId);
        await services.assets.UpdateAssetMarketInfo(assetId, isForSale, false, false, null, null);
        await services.assets.SetItemPrice(assetId, request.priceInRobux, request.priceInTickets);
    }

    // [HttpPost("assets/{assetId:long}/release")]
    // public async Task ReleaseAsset(long assetId)
    // {
    //     await services.assets.ValidatePermissions(assetId, userSession.userId);
    //     // services/api/src/controllers/proxy/v1/ItemConfiguration.ts:75
    //     throw new NotImplementedException();
    // }

    [HttpGetBypass("/v1/assets/restrictions")]
    [HttpGet("assets/restrictions")]
    public async Task<RobloxCollection<ItemRestrictions>> GetAssetRestrictions(string assetIds)
    {
        var parsed = assetIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (parsed.Count is > 200 or < 0) throw new BadRequestException(0, "Invalid asset id list");

        var results = (await services.assets.MultiGetAssetRestrictions(parsed)).ToList();

        if (parsed.Count != results.Count) {
            var returnedIds = results.Select(r => r.assetId).ToList();
            foreach (var missingId in parsed.Where(id => !returnedIds.Contains(id))) {
                results.Add(new ItemRestrictions {
                    assetId = missingId,
                    isLimited = false,
                    isLimitedUnique = false,
                    exists = false,
                });
            }
        }

        return new()
        { 
            data = results,
        };
    }

    [HttpGetBypass("/v1/creations/get-assets")]
    [HttpGet("creations/get-assets")]
    public async Task<RobloxCollectionPaginated<CreationEntry>> GetUserCreations(Models.Assets.Type assetType, int limit = 10, string? cursor = null, long? groupId = null)
    {
        var offset = int.Parse(cursor ?? "0");
        if (limit is > 100 or < 1) limit = 10;
        List<CreationEntry> result;
        if (groupId == null || groupId.Value == 0)
        {
            result = (await services.assets.GetCreations(CreatorType.User, safeUserSession.userId, assetType, offset, limit)).ToList();
        }
        else
        {
            result = (await services.assets.GetCreations(CreatorType.Group, groupId.Value, assetType, offset, limit)).ToList();
        }
        return new()
        {
            nextPageCursor = result.Count >= limit ? (offset + limit).ToString() : null,
            previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
            data = result,
        };
    }
    [HttpGetBypass("/v1/item-tags/metadata")]
    [HttpGet("item-tags/metadata")]
    public dynamic GetItemTagsMetadata()
    {
        return new
        {
            isItemTagsFeatureEnabled = false,
            enabledAssetTypes = new List<string>()
            {
                "Hat", "HairAccessory", "FaceAccessory", "NeckAccessory", "ShoulderAccessory", "BackAccessory",
                "FrotnAccessory", "WaistAccessory",
            },
            maximumItemTagsPerItem = 5,
        };
    }

    // todo: figure out what these are... (some sort of searching metadata?)
    [HttpGetBypass("/v1/item-tags")]
    [HttpGet("item-tags")]
    public dynamic GetTagsNoOp(string ItemIds)
    {
        // ItemIds is csv of "AssetId:21070012"
        return new
        {
            data = new List<int>(),
        };
    }
}