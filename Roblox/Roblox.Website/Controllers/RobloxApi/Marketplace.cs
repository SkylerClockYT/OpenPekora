using Roblox.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Roblox.Dto.Games;
using Roblox.Exceptions;
using Roblox.Models.Assets;
using Roblox.Services.App.FeatureFlags;
using BadRequestException = Roblox.Exceptions.BadRequestException;
using MultiGetEntry = Roblox.Dto.Assets.MultiGetEntry;
using Type = Roblox.Models.Assets.Type;
using Roblox.Logging;

namespace Roblox.Website.Controllers 
{
    // im sorry shika but i had to reformat because when i copied and pasted some code that shit broke
    [ApiController]
    [Route("/")]
    public class Marketplace : ControllerBase 
    {
        [HttpGetBypass("marketplace/productinfo")]
        public async Task<dynamic> GetProductInfo(long assetId) 
        {
            try 
            {
                var details = await services.assets.GetAssetCatalogInfo(assetId);
                long remaining = 0;
                
                if (details.itemRestrictions.Contains("Limited") ||
                    details.itemRestrictions.Contains("LimitedUnique")) {
                    var resale = await services.assets.GetResaleData(assetId);
                    remaining = resale.numberRemaining;
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
                        CreatorType = details.creatorType,
                        CreatorTargetId = details.creatorTargetId
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
                    Remaining = remaining,
                    MinimumMembershipLevel = 0
                };
            }
            catch (RecordNotFoundException) 
            {
                return Redirect($"https://economy.roblox.com/v2/assets/{assetId}/details");
            };
        }

        [HttpGetBypass("v2/assets/{assetId:long}/details")]
        public async Task<dynamic> GetProductInfoNew(long assetId)
        {
            long Remaining = 0;
            var details = await services.assets.GetAssetCatalogInfo(assetId);
            if (details.itemRestrictions.Contains("Limited") || details.itemRestrictions.Contains("LimitedUnique"))
            {
                var resale = await services.assets.GetResaleData(assetId);
                Remaining = resale.numberRemaining;
            }
            try
            {
                // this has gotta be a Type somewhere right
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
                        CreatorType = details.creatorType,
                        CreatorTargetId = details.creatorTargetId
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
            catch (RecordNotFoundException)
            {
                return Redirect($"https://economy.roproxy.com/v2/assets/{assetId}/details");
            }
        }

        // client here
        [HttpPostBypass("marketplace/submitpurchase")]
        public async Task<dynamic> SubmitPurchase([FromForm] Dto.Marketplace.ProductPurchaseRequest purchaseRequest)
        {
            var userId = safeUserSession.userId;
            FeatureFlags.FeatureCheck(FeatureFlag.EconomyEnabled);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // some sanity checks

            var productInfo = await services.games.GetDeveloperProductInfoFull(purchaseRequest.productId);
            if (!productInfo.isForSale)
                throw new BadRequestException(0, "Developer Product is not for sale");
            var iconModStatus = await services.assets.GetAssetModerationStatus(productInfo.iconImageAssetId);
            if (iconModStatus != ModerationStatus.ReviewApproved)
                throw new BadRequestException(0, "Developer Product is not approved");
            var uni = (await services.games.MultiGetUniverseInfo(new[] {productInfo.universeId})).ToList();
            if (uni.FirstOrDefault() is null || uni.First().rootPlaceId != purchaseRequest.placeId)
                throw new BadRequestException(0, "Place is invalid for this purchase or does not exist");
            if (productInfo.price != purchaseRequest.expectedUnitPrice)
                throw new BadRequestException(0, "Expected price is not the actual price");

            var receiptId = await services.users.PurchaseDeveloperProduct(userId, purchaseRequest.productId);
            stopwatch.Stop();
            Metrics.EconomyMetrics.ReportItemPurchaseTime(stopwatch.ElapsedMilliseconds,
                false);
            return new
            {
                success = true,
                status = "Bought",
                receipt = receiptId
            };
        }

        [HttpPostBypass("marketplace/purchase")]
        public async Task<dynamic> PurchaseProductMarket([FromForm] Dto.Marketplace.PurchaseRequest purchaseRequest)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.EconomyEnabled);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // some sanity checks
            Console.WriteLine(purchaseRequest.productId);
            Console.WriteLine(purchaseRequest.locationId);
            var productInfo = await services.assets.GetProductForAsset(purchaseRequest.productId);
            if (purchaseRequest.productId is 0 or < 0)
                purchaseRequest.productId = 0;
            if (productInfo.isLimited || productInfo.isLimitedUnique) 
                throw new BadRequestException(0, "Cannot purchase limited or limited unique items through this endpoint");
            await services.users.PurchaseNormalItem(safeUserSession.userId, purchaseRequest.productId,
                purchaseRequest.currencyTypeId);
            stopwatch.Stop();
            Metrics.EconomyMetrics.ReportItemPurchaseTime(stopwatch.ElapsedMilliseconds,
                false);
            return new
            {
                success = true,
                status = "Bought",
                receipt = "test"
            };
        }

        // look for dev prod, if not, look for normal asset, if not 400
        [HttpGetBypass("marketplace/productdetails")]
        public async Task<dynamic> GetProductDetailsMarketplace(long productId)
        {
            // based off of
            // https://web.archive.org/web/20220707014309/https://api.roblox.com/marketplace/productDetails?productId=19804017
            // and
            // https://web.archive.org/web/20171112192130/http://api.roblox.com/Marketplace/Productinfo?assetid=1149615185
            // (where it's not a developer product)
            try
            {
                var details = await services.games.GetDeveloperProductInfoFull(productId);
                return new
                {
                    // on roblox this usually leads to the id of a random shirt template??? LMFAOOOO
                    // idfk why and i dont really care to figure out cuz i dont think its necessary
                    // so for convenience sake of anyone using this api im putting the universe id
                    TargetId = details.universeId,
                    AssetId = 0,
                    ProductId = details.id,
                    ProductType = "Developer Product",
                    Name = details.name,
                    Description = details.description,
                    AssetTypeId = 0,
                    Creator = new
                    {
                        Id = 0,
                        Name = (string?)null,
                        // once again roblox api is weird
                        // these are usually null and 0 respectively but
                        // for convenience sakes ive set to the actual values
                        CreatorType = details.creatorType,
                        CreatorTargetId = details.creatorId
                    },
                    IconImageAssetId = details.iconImageAssetId,
                    Created = details.createdAt,
                    Updated = details.updatedAt,
                    PriceInRobux = details.price,
                    PriceInTickets = (int?)null,
                    Sales = details.sales,
                    IsNew = details.createdAt.Add(TimeSpan.FromDays(1)) < DateTime.Now,
                    IsForSale = details.isForSale,
                    IsPublicDomain = details.isForSale && details.price == 0,
                    IsLimited = false,
                    IsLimitedUnique = false,
                    Remaining = (int?)null,
                    MinimumMembershipLevel = 0
                };
            }
            catch (RecordNotFoundException)
            {
                var asset = await services.assets.DoesAssetExistType(productId);
                if (asset.exists)
                {
                    switch (asset.assetType)
                    {
                        case (int)Type.GamePass:
                            return Redirect($"/marketplace/game-pass-product-info?gamePassId={productId}");
                        default:
                            return Redirect($"/marketplace/productinfo?assetId={productId}");
                    }
                }
            }




            
            throw new BadRequestException(0, "Asset " + productId + " does not exist.");
        }

        // Studio
        [HttpGetBypass("marketplace/game-pass-product-info")]
        public async Task<dynamic> GetPassInfo(long gamePassId)
        {
            // based off of this
            // https://web.archive.org/web/20211201073809/https://api.roblox.com/marketplace/game-pass-product-info?gamePassId=12828275

            var details = await services.assets.GetAssetCatalogInfo(gamePassId);

            if (details.assetType != Type.GamePass)
                throw new BadRequestException(0, "Asset " + gamePassId + " is not a Game Pass");
            

            var gamePassDetails = await services.games.GetGamePassInfo(gamePassId);
            return new
            {
                TargetId = await services.games.GetRootPlaceId(gamePassDetails.universeId),
                ProductType = "Game Pass",
                AssetId = details.id,
                ProductId = details.id,
                Name = details.name,
                Description = details.description,
                AssetTypeId = (int)details.assetType, 
                Creator = new
                {
                    Id = details.creatorTargetId,
                    Name = details.creatorName,
                    CreatorType = details.creatorType,
                    CreatorTargetId = details.creatorTargetId
                },
                IconImageAssetId = details.id,
                Created = details.createdAt,
                Updated = details.updatedAt,
                PriceInRobux = details.price,
                PriceInTickets = details.priceTickets,
                Sales = details.saleCount,
                IsNew = details.createdAt.Add(TimeSpan.FromDays(1)) < DateTime.Now,
                IsForSale = details.isForSale,
                IsPublicDomain = details.isForSale && details.price == 0,
                IsLimited = false,
                IsLimitedUnique = false,
                Remaining = 0,
                MinimumMembershipLevel = 0,
                ContentRatingTypeId = 0
            };
        }

        [HttpPostBypass("marketplace/validatepurchase")]
        public async Task<ReceiptResponse> ValidatePurchase(Guid receipt)
        {
            if (!isRCC)
                throw new UnauthorizedException();
            Console.WriteLine($"validatepurchase RCC: {isRCC}, {receipt}");


            var productReceipt = await services.games.GetProductReceipt(receipt);
            if (productReceipt == null)
                throw new BadRequestException(0, "Receipt is invalid or does not exist.");

            return new ReceiptResponse
            {
                playerId = productReceipt.userId,
                placeId = currentPlaceId,
                isValid = productReceipt.processed,
                productId = productReceipt.productId,
            };
        }
        
        [HttpGetBypass("gametransactions/getpendingtransactions")]
        public async Task<dynamic> GetPendingTransactions(long placeId, long playerId)
        {
            if (!isRCC)
                throw new UnauthorizedException();

            var universeId = await services.games.GetUniverseId(placeId);
            var pendingReceipts = await services.games.GetPendingProductReceipts(playerId, universeId);

            if (pendingReceipts is null)
                return Array.Empty<dynamic>();

            return pendingReceipts.Select(pendingReceipt => new
            {
                playerId,
                placeId,
                receipt = pendingReceipt.id,
                actionArgs = new List<dynamic>
                {
                    new
                    {
                        Key = "productId",
                        Value = pendingReceipt.productId
                    },
                    new
                    {
                        Key = "currencyTypeId",
                        Value = 1
                    },
                    new
                    {
                        Key = "unitPrice",
                        Value = pendingReceipt.price
                    }
                }
            }).ToArray();
        }


        [HttpPostBypass("gametransactions/settransactionstatuscomplete")]
        public async Task<dynamic> ProcessTransaction()
        {
            if (!isRCC)
                throw new UnauthorizedException();
            string[] body = (await GetRequestBody()).Split("=");
            Guid receiptId = Guid.Parse(body[1]);

            var receipt = await services.games.GetProductReceipt(receiptId);

            if (receipt == null)
                throw new BadRequestException(0, "Receipt is invalid or does not exist.");
            if (receipt.processed)
                throw new BadRequestException(0, "Receipt has already been processed.");

            await services.games.ProcessProductReceipt(receiptId);

            return new
            {
                success = true
            };
        }
    }
}