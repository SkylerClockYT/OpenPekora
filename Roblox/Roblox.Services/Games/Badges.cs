using Dapper;
using Roblox.Dto.Games;
using Roblox.Models.Assets;
using Roblox.Models.Db;
using Roblox.Services.DbModels;

namespace Roblox.Services.Games;

public class BadgesService : ServiceBase, IService 
{
    
    public async Task<IEnumerable<BadgeAssetDetails>> GetBadgesForUniverse(Universe universe, int limit, int offset, SortOrder? sort)
    {
        var qu = await db.QueryAsync<BadgeAssetDetailsDb>(
    @"SELECT a.id, a.name, a.description, ab.enabled,
    (
        SELECT COUNT(*) FROM user_asset AS ua
        WHERE ua.asset_id = a.id
    ) as awardedCount,
    (
        SELECT COUNT(*) FROM user_asset AS ua
        WHERE ua.asset_id = a.id
        AND ua.created_at >= NOW() - INTERVAL '1 day'
    ) as pastDayAwardedCount,
    a.created_at as created,
    a.moderation_status as moderationStatus,
    a.updated_at as updated,
    (
        SELECT COUNT(*) FROM asset_play_history AS aph
        WHERE aph.asset_id = :rootPlaceId   
        AND aph.created_at >= NOW() - INTERVAL '1 day'
    ) as pastDayUniverseVisitors
    FROM asset AS a
    INNER JOIN asset_badge ab ON ab.asset_id = a.id
    WHERE ab.universe_id = :universeId
    LIMIT :limit OFFSET :offset",
    new { universe.rootPlaceId, universeId = universe.id, limit, offset });

        return qu.Select(c => new BadgeAssetDetails()
        {
            id = c.id,
            name = c.name,
            description = c.description,
            displayName = c.name,
            displayDescription = c.description,
            enabled = c.enabled && c.moderationStatus == ModerationStatus.ReviewApproved,
            iconImageId = c.id,
            displayIconImageId = c.id,
            moderationStatus = c.moderationStatus,
            created = c.created,
            updated = c.updated,
            statistics = new BadgeStatistics 
            {
                awardedCount = c.awardedCount,
                pastDayAwardedCount = c.pastDayAwardedCount,
                winRatePercentage = c.pastDayUniverseVisitors == 0 || c.pastDayAwardedCount == 0 ? 0 : Math.Round((decimal)c.pastDayAwardedCount / c.pastDayUniverseVisitors * 100, 1)
            },
            awardingUniverse = new BadgeAwardingUniverse 
            {
                id = universe.id,
                name = universe.sourceName,
                rootPlaceId = universe.rootPlaceId
            }
        });
    }
    public async Task<IEnumerable<BadgeAssetDetails>> GetBadgesForUser(
        long userId, int limit, int offset, SortOrder? sort)
    {
        var sql = @"
        WITH recent_badge_awards AS (
            SELECT ua.asset_id, COUNT(*) AS pastDayAwardedCount
            FROM user_asset ua
            WHERE ua.updated_at >= NOW() - INTERVAL '1 day'
            GROUP BY ua.asset_id
        ),
        recent_universe_visitors AS (
            SELECT uv.root_asset_id, COUNT(*) AS pastDayUniverseVisitors
            FROM asset_play_history aph
            JOIN universe uv ON uv.root_asset_id = aph.asset_id
            WHERE aph.created_at >= NOW() - INTERVAL '1 day'
            GROUP BY uv.root_asset_id
        )
        SELECT 
            a.id,
            a.name,
            a.description,
            ab.enabled,
            a.sale_count AS awardedCount,
            a.created_at AS created,
            a.moderation_status AS moderationStatus,
            ab.universe_id AS universeId,
            uv.root_asset_id AS rootPlaceId,
            a.updated_at AS updated,
            COALESCE(rba.pastDayAwardedCount, 0) AS pastDayAwardedCount,
            COALESCE(ruv.pastDayUniverseVisitors, 0) AS pastDayUniverseVisitors,
            ass.name AS universeName
        FROM asset a
        INNER JOIN asset_badge ab ON ab.asset_id = a.id
        INNER JOIN user_asset ua ON ua.asset_id = a.id AND ua.user_id = :userId
        INNER JOIN universe uv ON uv.id = ab.universe_id
        LEFT JOIN asset ass ON ass.id = uv.root_asset_id
        LEFT JOIN recent_badge_awards rba ON rba.asset_id = a.id
        LEFT JOIN recent_universe_visitors ruv ON ruv.root_asset_id = uv.root_asset_id
        ORDER BY a.created_at DESC
        LIMIT :limit OFFSET :offset;
    ";

        var rows = await db.QueryAsync<BadgeAssetDetailsDb>(sql, new
        {
            userId,
            limit,
            offset
        });

        return rows.Select(c => new BadgeAssetDetails()
        {
            id = c.id,
            name = c.name,
            description = c.description,
            displayName = c.name,
            displayDescription = c.description,
            enabled = c.enabled && c.moderationStatus == ModerationStatus.ReviewApproved,
            iconImageId = c.id,
            displayIconImageId = c.id,
            moderationStatus = c.moderationStatus,
            created = c.created,
            updated = c.updated,
            statistics = new BadgeStatistics
            {
                awardedCount = c.awardedCount,
                pastDayAwardedCount = c.pastDayAwardedCount,
                winRatePercentage = (c.pastDayAwardedCount == 0 || c.pastDayUniverseVisitors == 0)
                    ? 0
                    : Math.Round((decimal)c.pastDayAwardedCount / c.pastDayUniverseVisitors, 1)
            },
            awardingUniverse = new BadgeAwardingUniverse
            {
                id = c.universeId!.Value,
                name = c.universeName!,
                rootPlaceId = c.rootPlaceId!.Value
            }
        });
    }

    public async Task<IEnumerable<BadgeAssetDetails>> GetBadgeInfoExtended(long assetId, Universe universe, int limit,
        int offset, SortOrder? sort)
    {
        var qu = await db.QueryAsync<BadgeAssetDetailsDb>(
            @"SELECT a.id, a.name, a.description, ab.enabled,
            a.sale_count as awardedCount,
            a.created_at as created,
            a.moderation_status as moderationStatus,
            a.updated_at as updated,
            (
                SELECT COUNT(*) FROM user_asset AS ua
                                WHERE ua.asset_id = a.id
                                AND ua.updated_at >= NOW() - INTERVAL '1 day'
            ) as pastDayAwardedCount,
            (
                SELECT COUNT(*) FROM asset_play_history as aph
                                WHERE aph.asset_id = :rootPlaceId   
                                AND aph.created_at >= NOW() - INTERVAL '1 day'
            ) as pastDayUniverseVisitors
            FROM asset AS a
            INNER JOIN asset_badge ab ON ab.asset_id = a.id
            WHERE a.id = :assetId
            LIMIT :limit OFFSET :offset",
            new
            {
                assetId,
                rootPlaceId = universe.rootPlaceId,
                limit,
                offset,
            });
        return qu.Select(c => new BadgeAssetDetails()
        {
            id = c.id,
            name = c.name,
            description = c.description ?? "",
            displayName = c.name,
            displayDescription = c.description ?? "",
            enabled = c.enabled && c.moderationStatus == ModerationStatus.ReviewApproved,
            iconImageId = c.id,
            displayIconImageId = c.id,
            moderationStatus = c.moderationStatus,
            created = c.created,
            updated = c.updated,
            statistics = new BadgeStatistics {
                awardedCount = c.awardedCount,
                pastDayAwardedCount = c.pastDayAwardedCount,
                winRatePercentage = c.pastDayUniverseVisitors == 0 ? 0 : Math.Round((decimal)c.pastDayAwardedCount / c.pastDayUniverseVisitors, 1)
            },
            awardingUniverse = new BadgeAwardingUniverse {
                id = universe.id,
                name = universe.name,
                rootPlaceId = universe.rootPlaceId
            }
        });
    }
    
    public async Task<IEnumerable<BadgeAwardDate>> GetUserBadgeAwardedDates(long userId, long[] badgeIds)
    {

        var builder = new SqlBuilder();
        var template = builder.AddTemplate(
            @"SELECT 
            ua.updated_at as awardedDate,
            ab.asset_id as badgeId
            FROM asset_badge AS ab
            INNER JOIN user_asset ua ON ua.asset_id = ab.asset_id
            /**where**/
            LIMIT 1000"
            );
        builder.Where("ua.user_id = " + userId);
        builder.Where("ab.asset_id IN (" + string.Join(",", badgeIds) + ")");
        return await db.QueryAsync<BadgeAwardDate>(template.RawSql, template.Parameters);
    }
    public async Task<BadgeDetails?> GetBadgeInfo(long assetId) 
    {
        var qu = await db.QuerySingleOrDefaultAsync<BadgeDetails?>(
            @"SELECT 
            ab.asset_id as assetId, 
            ab.universe_id as universeId,
            ab.enabled as enabled
            FROM asset_badge AS ab
            WHERE asset_id = :assetId
            LIMIT 1",
            new { assetId });
            
        if (qu == null) 
            return null;

        using var assets = ServiceProvider.GetOrCreate<AssetsService>(this);

        var modStatus = await assets.GetAssetModerationStatus(assetId);
        return new BadgeDetails 
        {
            assetId = qu.assetId,
            universeId = qu.universeId,
            enabled = qu.enabled && modStatus == ModerationStatus.ReviewApproved
        };
    }
    
    public async Task UpdateBadge(long badgeId, bool enabled)
    {
        await db.ExecuteAsync("UPDATE asset_badge SET enabled = :enabled WHERE asset_id = :badgeId", new
        {
            badgeId,
            enabled
        });
    }
    public string GetDifficultyFromPercentage(decimal percentage)
    {
        if (percentage >= 90 && percentage <= 100) return "Freebie";
        if (percentage >= 80 && percentage < 90) return "Cake Walk";
        if (percentage >= 50 && percentage < 80) return "Easy";
        if (percentage >= 30 && percentage < 50) return "Moderate";
        if (percentage >= 20 && percentage < 30) return "Challenging";
        if (percentage >= 10 && percentage < 20) return "Hard";
        if (percentage >= 5 && percentage < 10) return "Extreme";
        if (percentage >= 1 && percentage < 5) return "Insane";
        if (percentage >= 0 && percentage < 1) return "Impossible";

        return "Unknown";
    }



    public bool IsThreadSafe()
    {
        return true;
    }

    public bool IsReusable()
    {
        return false;
    }
}