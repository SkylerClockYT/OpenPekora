using System.Diagnostics;
using System.Text.RegularExpressions;
using Dapper;
using Dapper.Contrib.Extensions;
using InfluxDB.Client.Api.Domain;
using Roblox.Dto.Assets;
using Roblox.Dto.Forums;
using Roblox.Services.App.FeatureFlags;
using Roblox.Services.Exceptions;
namespace Roblox.Services;

public class PromocodesService : ServiceBase, IService
{
    public class Rewards
    {
        public int? robux { get; set; }
        public long? assetId { get; set; }
    }
    public async Task AddPromocode(string promocode, int? robux, long? assetId)
    {
        await InsertAsync("asset_promocodes", new
        {
            asset_id = assetId,
            robux,
            promocode,
        });
    }
    public async Task DeletePromocode(string promocode)
    {
        await db.ExecuteAsync("DELETE FROM asset_promocodes WHERE promocode = :promocode", new
        {
            promocode,
        });
    }
    public async Task<Rewards> GetRewards(string promocode)
    {
        return await db.QuerySingleOrDefaultAsync<Rewards>("SELECT asset_id as assetId, robux FROM asset_promocodes WHERE promocode = :promocode", new
        {
            promocode,
        });
    }

    public async Task<bool> IsPromocodeClaimed(string promocode, long userId)
    {
        return await db.QueryFirstOrDefaultAsync<bool>("SELECT 1 FROM user_asset_promocodes WHERE code = :promocode AND user_id = :userId", new
        {
            promocode,
            userId,
        });
    }
    public async Task<Rewards> ClaimPromocode(string promocode, long userId)
    {
        Rewards reward = await GetRewards(promocode);
        if (reward == null)
            throw new RecordNotFoundException("Invalid promocode");
        await InTransaction(async (t) =>
        {
            if (await IsPromocodeClaimed(promocode, userId))
                throw new RecordNotFoundException("Promocode already claimed");
            // If this failes 
            await InsertAsync("user_asset_promocodes", new
            {
                user_id = userId,
                code = promocode,
            });

            if (reward.assetId != null)
            {
                UsersService users = ServiceProvider.GetOrCreate<UsersService>();
                // Double check if the user already owns the asset
                var ownedCopies = (await users.GetUserAssets(userId, reward.assetId.Value)).ToList();
                if (ownedCopies.Count == 0)
                {
                    await db.QuerySingleOrDefaultAsync(
                        "INSERT INTO user_asset (asset_id, user_id, serial) VALUES (:asset_id, :user_id, :serial) RETURNING user_asset.id", new
                        {
                            asset_id = reward.assetId,
                            user_id = userId,
                            serial = 0,
                        });
                }
            
            }
            if (reward.robux != null)
            {
                EconomyService ec = ServiceProvider.GetOrCreate<EconomyService>();
                await ec.IncrementCurrency(Models.Assets.CreatorType.User, userId, Models.Economy.CurrencyType.Robux, (int)reward.robux);
            }
            return 0;
        });
        return reward;
    }
    public bool IsReusable()
    {
        return true;
    }

    public bool IsThreadSafe()
    {
        return true;
    }
}
