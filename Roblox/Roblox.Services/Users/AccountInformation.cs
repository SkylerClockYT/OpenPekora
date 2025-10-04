using System.Diagnostics;
using Dapper;
using Roblox.Dto.Users;
using Roblox.Models.Users;
using BadgeEntry = Roblox.Dto.Users.BadgeEntry;

namespace Roblox.Services;

public class AccountInformationService : ServiceBase, IService
{
    public async Task<IEnumerable<BadgeEntry>> GetUserBadges(long userId)
    {
        var result = await db.QueryAsync<BadgeEntry>("SELECT badge_id as id FROM user_badge WHERE user_id = :user_id",
            new
            {
                user_id = userId,
            });
        return result.Select(c =>
        {
            var meta = Models.Users.BadgesMetadata.Badges.Find(v => v.id == c.id);
            Debug.Assert(meta != null);
            c.name = meta.name;
            c.description = meta.description;
            return c;
        });
    }

    public async Task<UserSettingsEntry> GetUserSettings(long userId)
    {
        return await db.QuerySingleOrDefaultAsync<UserSettingsEntry>("SELECT gender, theme, inventory_privacy as inventoryPrivacy, trade_privacy as tradePrivacy, trade_filter as tradeFilter, private_message_privacy as privateMessagePrivacy, avatar_page_style as avatarPageStyle FROM user_settings WHERE user_id = :user_id",
            new {user_id = userId});
    }

    public async Task<Gender> GetUserGender(long userId)
    {
        return (await GetUserSettings(userId)).gender;
    }
    
    public async Task SetUserGender(long userId, Gender newGender)
    {
        await UpdateAsync("user_settings", "user_id", userId, new
        {
            gender = newGender,
        });
    }

    public async Task<ThemeTypes> GetUserTheme(long userId)
    {
        using var themeCache = ServiceProvider.GetOrCreate<UserThemeCache>();
        var (exists, cached) = themeCache.Get(userId);
        if (exists)
            return cached;
        
        cached = (await GetUserSettings(userId)).theme;
        themeCache.Set(userId, cached);
        return cached;
    }
    
    public async Task SetUserTheme(long userId, ThemeTypes theme)
    {
        await UpdateAsync("user_settings", "user_id", userId, new
        {
            theme = theme,
        });
        using var themeCache = ServiceProvider.GetOrCreate<UserThemeCache>();
        themeCache.Set(userId, theme);
    }
    
    public async Task<AvatarPageStyle> GetUserAvatarPageStyle(long userId)
    {
        using var styleCache = ServiceProvider.GetOrCreate<UserAvatarPageStyleCache>();
        var (exists, cached) = styleCache.Get(userId);
        if (exists)
            return cached;
        
        cached = (await GetUserSettings(userId)).avatarPageStyle;
        styleCache.Set(userId, cached);
        return cached;
    }
    
    public async Task SetUserAvatarPageStyle(long userId, AvatarPageStyle style)
    {
        await UpdateAsync("user_settings", "user_id", userId, new
        {
            avatar_page_style = style,
        });
        using var styleCache = ServiceProvider.GetOrCreate<UserAvatarPageStyleCache>();
        styleCache.Set(userId, style);
    }

    public async Task<InventoryPrivacy> GetUserInventoryPrivacy(long userId)
    {
        return (await GetUserSettings(userId)).inventoryPrivacy;
    }
    
    public async Task SetUserInventoryPrivacy(long userId, InventoryPrivacy privacy)
    {
        await UpdateAsync("user_settings", "user_id", userId, new
        {
            inventory_privacy = privacy,
        });
    }
    
    public async Task<GeneralPrivacy> GetUserTradePrivacy(long userId)
    {
        return (await GetUserSettings(userId)).tradePrivacy;
    }
    
    public async Task SetUserTradePrivacy(long userId, GeneralPrivacy privacy)
    {
        await UpdateAsync("user_settings", "user_id", userId, new
        {
            trade_privacy = privacy,
        });
    }
    
    public async Task<TradeQualityFilter> GetUserTradeValue(long userId)
    {
        return (await GetUserSettings(userId)).tradeFilter;
    }
    
    public async Task SetUserTradeValue(long userId, TradeQualityFilter filter)
    {
        await UpdateAsync("user_settings", "user_id", userId, new
        {
            trade_filter = filter,
        });
    }
    public async Task<GeneralPrivacy> GetUserPrivateMessagePrivacy(long userId)
    {
        return (await GetUserSettings(userId)).privateMessagePrivacy;
    }
    
    public async Task SetUserPrivateMessagePrivacy(long userId, GeneralPrivacy privacy)
    {
        await UpdateAsync("user_settings", "user_id", userId, new
        {
            private_message_privacy = privacy,
        });
    }
    
    public async Task<UserConnections> GetUserConnections(long userId)
    {
        return await db.QuerySingleOrDefaultAsync<UserConnections>(@"
                SELECT discord, twitter, telegram, tiktok, youtube, twitch, github, roblox
                FROM user_connections WHERE user_id = :userId",
            new {userId});
    }
    
    public async Task SetUserConnections(long userId, UserConnections connections)
    {
        var userConnectionCount = await db.QuerySingleOrDefaultAsync<int>(@"
                SELECT COUNT(*) FROM user_connections WHERE user_id = :userId",
            new {userId});
        if (userConnectionCount == 0)
        {
            await InsertAsync("user_connections", "user_id", new
            {
                user_id = userId,
                discord = connections.discord,
                tiktok = connections.tiktok,
                twitch = connections.twitch,
                youtube = connections.youtube,
                telegram = connections.telegram,
                twitter = connections.twitter,
                github = connections.github,
                roblox = connections.roblox,
            });
        }
        else
        {
            await UpdateAsync("user_connections", "user_id", userId, connections);
        }
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