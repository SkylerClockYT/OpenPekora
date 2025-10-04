using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Games;
using Roblox.Services.Exceptions;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/api")]
public class ApiController : ControllerBase
{

    [HttpGet("users/get-by-username")]
    public async Task<dynamic> GetUserByUsername(string username)
    {
        var result = (await services.users.MultiGetUsersByUsername(new[] { username })).ToList();
        if (result.Count == 0) return new { success = false, errorMessage = "User not found" };
        var user = result[0];
        return new
        {
            Id = user.id,
            Username = user.name,
            AvatarUri = (string?)null,
            AvatarFinal = false,
            IsOnline = false,
        };
    }

    [HttpGet("users/{userId:long}")]
    public async Task<dynamic> GetUserById(long userId)
    {
        var result = await services.users.GetUserById(userId);
        return new
        {
            Id = result.userId,
            Username = result.username,
            AvatraUri = (string?)null,
            AvatarFile = false,
            IsOnline = false,
        };
    }

    [HttpGet("v1/countries/phone-prefix-list")]
    public dynamic GetCountries()
    {
        return new List<dynamic>()
        {
            new
            {
                name = "United States",
                code = "US",
                prefix = "1",
                localizedName = "United States",
            },
            // from services/api/src/controllers/proxy/v1/Api.ts:38
            new
            {
                name = "Your Mom",
                code = "YM",
                prefix = "69",
                localizedName = "Your Mom",
            }
        };
    }

    [HttpGet("marketplace/productinfo")]
    public async Task<dynamic> GetProductInfo(long assetId)
    {
        // if (await services.games.GetDeveloperProductCountId(assetId) > 0) {
        //     return Redirect($"/marketplace/productdetails?productId={assetId}");
        // }
        long Remaining = 0;
        var details = await services.assets.GetAssetCatalogInfo(assetId);
        if(details.itemRestrictions.Contains("Limited") || details.itemRestrictions.Contains("LimitedUnique"))
        {
            var resale = await services.assets.GetResaleData(assetId);
            Remaining = resale.numberRemaining;
        }
        return new
        {
            TargetId = details.id,
            AssetId = details.id,
            ProductId = details.id, 
            Name = details.name,
            Description = details.description,
            AssetTypeId = (int)details.assetType,
            Creator = new
            {
                Id = details.creatorTargetId,
                Name = details.creatorName,
            },  
            IconImageAssetId = 0,
            Created = details.createdAt,
            Updated = details.updatedAt,
            PriceInRobux = details.price,
            PriceInTickets = details.priceTickets,
            Sales = details.saleCount,
            IsNew = details.createdAt.Add(TimeSpan.FromDays(1)) < DateTime.Now,
            IsForSale = details.isForSale,
            IsPublicDomain = details.isForSale && details.price == 0,
            IsLimited = details.itemRestrictions.Contains("Limited"),
            IsLimitedUnique = details.itemRestrictions.Contains("LimitedUnique"),
            Remaining,
            MinimumMembershipLevel = 0
        };
    }

    [HttpGet("alerts/alert-info")]
    public async Task<dynamic> GetAlert()
    {
        var alert = await services.users.GetGlobalAlert();
        return new
        {
            IsVisible = alert != null,
            Text = alert?.message ?? "",
            LinkText = "",
            LinkUrl = alert?.url ?? "",
        };
    }

    [HttpGet("v1/items/restrictions")]
    public async Task<dynamic> GetItemRestrictions(string assetIds)
    {
        var ids = assetIds.Split(",").Select(long.Parse).ToArray();
        if (!ids.Any())
            return Array.Empty<BadgeAwardDate>();
        return await services.assets.MultiGetAssetRestrictions(ids);
    }
}

