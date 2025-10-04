using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Models;
using Roblox.Exceptions;
using Roblox.Models.Db;
namespace Roblox.Website.Controllers
{
    [MVC.ApiController]
    [MVC.Route("/")]
    public class Inventory: ControllerBase
    {
        [HttpGetBypass("/v1/users/{userId}/items/{itemType}/{itemTargetId}")]
        public async Task <RobloxCollectionPaginated<dynamic>> GetOwnedItemsOfSpecificType(long userId, string itemType, long itemTargetId)
        {
            bool canViewItems = false;
            if (userSession != null)
                canViewItems = userId == userSession.userId;
            var assetType = services.assets.GetTypeFromPluralString(itemType);
            if (!canViewItems && (isRCC && assetType == Models.Assets.Type.GamePass || isRCC && assetType == Models.Assets.Type.Badge))
            {
                canViewItems = true;
            }
            // TODO: check if this is good.
            if (!canViewItems)
                throw new BadRequestException();
            
            var inventory = await services.inventory.GetInventory(userId, assetType, SortOrder.Asc, 100, 0);
            
            return new RobloxCollectionPaginated<dynamic>
            {
                previousPageCursor = (string?)null,
                nextPageCursor = (string?)null,
                data = inventory.Where(c => c.assetId == itemTargetId 
                                            // || c.assetTypeId == assetType TODO: not sure if this is necessary but also makes no sense considering how roblox uses it
                                            ).Select(c => new
               {
                    Id = c.assetId,
                    Name = c.name,
                    Type = (int)c.assetTypeId,
                    InstanceId = 0
                })
            };
        }
    }
}