using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Services;
namespace Roblox.Website.Pages.Internal;
// Need to improve this later
public class Promocodes : RobloxPageModel
{    
    public PromocodesService.Rewards reward;
    public string? errorMessage { get; set; }
    public string? successMessage { get; set; }
    [BindProperty]
    public string? promocode { get; set; }
    public void OnGet()
    {

    }
    public async Task OnPost()
    {
        if (string.IsNullOrWhiteSpace(promocode))
        {
            errorMessage = "Promocode is empty";
            return;
        }

        if (userSession == null)
        {
            errorMessage = "You must be logged in to claim a promocode";
            return;
        }
        try
        {
            reward = await services.promocodes.ClaimPromocode(promocode, userSession.userId);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return;
        }

        if (reward.assetId != null && reward.robux != null)
        {
            var assetInfo = await services.assets.GetAssetCatalogInfo((long)reward.assetId);
            successMessage = $"You have successfully claimed the item {assetInfo.name} and {reward.robux} Robux! Check your inventory to see it.";
        }
        else if (reward.assetId != null)
        {
            var assetInfo = await services.assets.GetAssetCatalogInfo((long)reward.assetId);
            successMessage = $"You have successfully claimed the item {assetInfo.name}! Check your inventory to see it.";
        }
        else if (reward.robux != null)
        {
            successMessage = $"You have successfully claimed {reward.robux} Robux!";
        }
        promocode = null;
    }
}