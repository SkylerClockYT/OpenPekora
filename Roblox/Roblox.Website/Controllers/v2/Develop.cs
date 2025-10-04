using Microsoft.AspNetCore.Mvc;
using Roblox.Models.Db;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/develop/v2")]
public class DevelopControllerV2 : ControllerBase
{
    [HttpGetBypass("/v2/assets/{assetId}/versions")]
    [HttpGet("assets/{assetId}/versions")]
    public async Task<dynamic> GetAssetVersions(long assetId, string? cursor, int limit = 10, SortOrder sortOrder = SortOrder.Desc)
    {
        await services.assets.ValidatePermissions(assetId, safeUserSession.userId);
        if (limit is < 1 or > 100) limit = 10;
        int offset = !string.IsNullOrWhiteSpace(cursor) ? int.Parse(cursor) : 0;
        var versions = (await services.assets.GetAssetVersions(assetId, offset, limit, sortOrder)).ToList();
        return new
        {
            previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
            nextPageCursor = versions.Count >= limit ? (offset + limit).ToString() : null,
            data = versions.Select(c => new
            {
                Id = c.assetVersionId,
                assetId = c.assetId,
                assetVersionNumber = c.versionNumber,
                creatorTargetId = c.creatorId,
                creatingUniverseId = (string?)null,
                created = c.createdAt,
                isEqualToCurrentPublishedVersion = c.contentUrl == versions.First().contentUrl,
                isPublished = true
            })
        };
    }

    [HttpGetBypass("/v2/universes/{universeId}/places")]
    [HttpGet("universes/{universeId}/places")]
    public async Task<dynamic> GetUniversePlaces(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var places = await services.games.GetUniversePlaces(universeId);
        var universe = await services.games.GetUniverseInfo(universeId);
        return new
        {
            previousPageCursor = (string?)null,
            nextPageCursor = (string?)null,
            data = places.Select(c => new
            {
                maxPlayerCount = c.maxPlayerCount,
                socialSlotType = "Automatic",
                customSocialSlotsCount = (string?)null,
                allowCopying = false,
                currentSavedVersion = 1,
                allowedGearTypes = (string?)null,
                maxPlayersAllowed = c.maxPlayerCount,
                created = c.created,
                updated = c.updated,
                id = c.placeId,
                universeId = universeId,
                name = c.name,
                description = c.description ?? "",
                isRootPlace = c.placeId == universe.rootPlaceId,

            })
        };
    }
    [HttpPostBypass("/v2/universes/{universeId}/shutdown")]
    [HttpPost("universes/{universeId}/shutdown")]
    public async Task<dynamic> ShutdownUniverse(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var places = await services.games.GetUniversePlaces(universeId);
        foreach (var place in places)
        {
            var gameServers = await services.gameServer.GetGameServersForPlace(place.placeId, 1);
            foreach (var server in gameServers)
            {
                await services.gameServer.ShutDownServerAsync(server.id);
            }
        }

        return new {};
    }
}