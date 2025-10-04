
using InfluxDB.Client.Core.Exceptions;
using Roblox;
using Roblox.Dto.Games;
using Roblox.Dto.Users;
using Roblox.Models.Assets;
using Roblox.Models.Games;
using Roblox.Models.GameServer;
using Roblox.Services;
using Roblox.Services.Signer;
namespace Roblox.Services.PlaceLauncher;
public class PlaceLauncherService : ServiceBase
{

    private static GamesService games = new GamesService();
    private static GameServerService gameServer = new GameServerService();
    private static UsersService users = new UsersService();
    private static SignService sign = new SignService();
    public enum MatchmakingContextId
    {
        Default = 1,
        Xbox,
        CloudEdit,
        CloudEditTest,
    }

    public async Task<PlaceLaunchResponse> PlaceLauncherAsync(PlaceLaunchRequest plRequest)
    {
        if (plRequest.username == null || plRequest.userId == null || plRequest.cookie == null)
            throw new ArgumentNullException("One of the arguments are missing");
        switch (plRequest.request)
        {
            case "RequestGameJob":
                if (plRequest.gameId == null)
                    throw new BadRequestException("Game Id is missing");
                return await RequestGameJob((long)plRequest.userId, (Guid)plRequest.gameId, plRequest.placeId);
            case "RequestGame":
                return await RequestGame(plRequest.placeId, (long)plRequest.userId, plRequest.cookie, plRequest.special, plRequest.username);
            case "CloudEdit":
                return await RequestCloudEdit(plRequest.placeId, (long)plRequest.userId, plRequest.username);
            case "RequestPrivateGame":
                break;
        }
        //default
        return new PlaceLaunchResponse()
        {
            status = (int)JoinStatus.Error,
            message = "An error occured while starting the game."
        };
    }

    public async Task<PlaceLaunchResponse> RequestGameJob(long userId, Guid gameId, long placeId)
    {
        var server = await gameServer.GetGameServer(gameId);
        if (server == null)
        {
            return new PlaceLaunchResponse()
            {
                status = (int)JoinStatus.Error,
                message = "The game server does not exist."
            };
        }
        // Create security ticket for the player
        using var playerSecurity = ServiceProvider.GetOrCreate<PlayerSecurityService>();
        await playerSecurity.CreatePlayerTicket(userId, server.id);

        if (await games.IsFull(gameId, placeId))
        {
            return new PlaceLaunchResponse()
            {
                jobId = gameId,
                status = (int)JoinStatus.GameFull,
                message = "The game is full."
            };
        }
        return new PlaceLaunchResponse()
        {
            jobId = gameId,
            status = (int)JoinStatus.Joining,
            joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={gameId}",
            authenticationUrl = $"{Roblox.Configuration.BaseUrl}/Login/Negotiate.ashx",
            authenticationTicket = "hi",
            message = $"Joining {gameId}",
        };
    }

    public async Task<PlaceLaunchResponse> RequestGame(long placeId, long userId, string cookie, bool? Special = false, string? username = null)
    {
        dynamic? joinScript = null;
        PlaceEntry placeInfo = (await games.MultiGetPlaceDetails(new[] { placeId })).First();
        if (placeInfo.moderationStatus != ModerationStatus.ReviewApproved || placeInfo.year == 2016)
        {
            return new PlaceLaunchResponse()
            {
                status = (int)JoinStatus.Error,
                message = "The game is not active."
            };
        }

        if (!await games.CanUserJoinUniverse(userId, placeInfo.builderId, placeInfo.universeId))
        {
            return new PlaceLaunchResponse()
            {
                status = (int)JoinStatus.Unauthorized,
                message = "You do not have permission to join this game."
            };
        }

        var result = await gameServer.GetServerForPlace(placeInfo, (int)MatchmakingContextId.Default);
        if (Special.HasValue && (bool)Special)
        {
            string membership = await users.GetUserMemberShipAsString(userId);
            var userInfo = await users.GetUserById((long)userId);
            var accountAgeDays = DateTime.UtcNow.Subtract(userInfo.created).Days;

            string characterAppearanceUrl = $"{Configuration.BaseUrl}/v1/avatar-fetch?userId={userId}&placeId={placeId}";
            GameServerDb jobInfo = await gameServer.GetGameServer(result.job);
            string clientTicket =  sign.GenerateClientTicket(placeInfo.year, userId, username!, characterAppearanceUrl, membership, result.job, accountAgeDays, placeId);
            joinScript = games.GetJoinScript(placeInfo, userInfo, jobInfo, characterAppearanceUrl, clientTicket, membership, accountAgeDays, true, cookie);

        }
        using var playerSecurity = ServiceProvider.GetOrCreate<PlayerSecurityService>();
        // Create security ticket for the player
        await playerSecurity.CreatePlayerTicket(userId, result.job);
        if (result.status == JoinStatus.Joining)
        {
            return new PlaceLaunchResponse()
            {
                jobId = result.job,
                status = (int)result.status,
                joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}",
                authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
                authenticationTicket = cookie,
                message = $"Server found ({result.job})",
                joinScript = (Special ?? false) ? joinScript ?? "" : ""
            };
        }
        return new PlaceLaunchResponse()
        {
            status = (int)JoinStatus.Loading,
            message = "Server found, loading...",
        };
    }
    public async Task<PlaceLaunchResponse> RequestCloudEdit(long placeId, long userId, string username)
    {
        string characterAppearanceUrl = $"{Configuration.BaseUrl}/v1.1/avatar-fetch?userId={userId}&placeId={placeId}";
        PlaceEntry placeInfo = (await games.MultiGetPlaceDetails(new[] { placeId })).First();
        // Block 2017 due to authentication issues
        if (placeInfo.moderationStatus != ModerationStatus.ReviewApproved || placeInfo.year == 2017)
        {
            return new PlaceLaunchResponse()
            {
                status = (int)JoinStatus.Error,
                message = "The game is not active."
            };
        }
        // Cloud edit check
        var canCloudEdit = await games.CanEditUniverse(userId, placeInfo.universeId) || placeInfo.builderId == userId;
        if (!canCloudEdit)
        {
            return new PlaceLaunchResponse()
            {
                status = (int)JoinStatus.Error,
                message = "You do not have permission to edit this place."
            };
        }

        var result = await gameServer.GetServerForPlace(placeInfo, (int)MatchmakingContextId.CloudEdit);
        // Create security ticket for the player
        using var playerSecurity = ServiceProvider.GetOrCreate<PlayerSecurityService>();
        await playerSecurity.CreatePlayerTicket(userId, result.job);

        if (result.status == JoinStatus.Joining)
        {
            string membership = await users.GetUserMemberShipAsString(userId);
            var userInfo = await users.GetUserById((long)userId);
            var accountAgeDays = DateTime.UtcNow.Subtract(userInfo.created).Days;
            GameServerDb jobInfo = await gameServer.GetGameServer(result.job);
            string clientTicket = sign.GenerateClientTicket(placeInfo.year, userId, username, characterAppearanceUrl, membership, result.job, accountAgeDays, placeId);

            dynamic settings = games.GetJoinScript(placeInfo, userInfo, jobInfo, characterAppearanceUrl, clientTicket, membership, accountAgeDays, false, null);
            return new PlaceLaunchResponse()
            {
                jobId = result.job,
                status = (int)result.status,
                joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}",
                authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
                settings = settings,
                authenticationTicket = "hi",
                message = $"Joining cloudedit session ({result.job})",
            };
        }
        return new PlaceLaunchResponse()
        {
            status = (int)JoinStatus.Loading,
            message = "Server found, loading...",
        };
    }
}