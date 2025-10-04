namespace Roblox.Services;

public class PlayerSecurityService :  ServiceBase, IService 
{
    #region Tickets
    private string GetPlayerTicketKey(long userId)
    {
        return "PlayerTicket:" + userId;
    }
    // 15 minutes is the default timeout for player tickets
    public async Task CreatePlayerTicket(long userId, Guid jobId)
    {
        await redis.StringSetAsync(GetPlayerTicketKey(userId), jobId.ToString(), TimeSpan.FromMinutes(15));
    }
    private async Task<string?> GetPlayerTicket(long userId)
    {
        return await redis.StringGetAsync(GetPlayerTicketKey(userId));
    }
    private async Task DeletePlayerTicket(long userId)
    {
        await redis.KeyDeleteAsync(GetPlayerTicketKey(userId));
    }
    public async Task<bool> IsPlayerTicketValid(long userId, Guid jobId)
    {
        string? ticket = await GetPlayerTicket(userId);
        if (ticket == null)
        {
            return false;
        }
        var isTicketValid = ticket == jobId.ToString();
        // Remove the ticket so it cannot be reused
        await DeletePlayerTicket(userId);
        return isTicketValid;
    }
    #endregion

    #region Join Validation
    
    public async Task<bool> ValidateTeleport(long originPlaceId, long destinationPlaceId)
    {
        using var games = Services.ServiceProvider.GetOrCreate<GamesService>(this);
        Console.WriteLine("Validating teleport from {0} to {1}", originPlaceId, destinationPlaceId);
        var destinationInfo = await games.GetUniverseInfo(await games.GetUniverseId(destinationPlaceId));
        var isSubPlace = destinationInfo.rootPlaceId != destinationPlaceId;
        if (originPlaceId == 0)
        {
            return isSubPlace ?
                false : // If the origin is 0, we can not teleport to a subplace
                true; // If the origin is 0, we can teleport to the root place of the universe
        }
        // Should be OK
        if (originPlaceId == destinationPlaceId)
        {
            return true;
        }

        var originInfo = await games.GetUniverseInfo(await games.GetUniverseId(originPlaceId));
        Console.WriteLine("Origin universe: {0}, Destination universe: {1}", originInfo.id, destinationInfo.id);
        // If the destination is a subplace does not belong to the same universe, we can not teleport
        if (isSubPlace && originInfo.id != destinationInfo.id)
        {
            return false;
        }
        // OK!
        return true;
    }

    #endregion
    public bool IsThreadSafe()
    {
        return true;
    }

    public bool IsReusable()
    {
        return true;
    }
}
