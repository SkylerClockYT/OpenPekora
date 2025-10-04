using Roblox.Dto.Users;
using Roblox.Models.Users;

namespace Roblox.Services;


public class UserSessionsCache : GenericMemoryCache<string, SessionEntry?>
{
    
}

public class UserInviteCache : GenericMemoryCache<long, UserInviteEntry?>
{
    public UserInviteCache() : base(TimeSpan.FromMinutes(5))
    {
        
    }
}

public class UserApplicationCache : GenericMemoryCache<long, UserApplicationEntry?>
{
    public UserApplicationCache() : base(TimeSpan.FromMinutes(5))
    {
        
    }
}

public class GetUserByIdCache : GenericMemoryCache<long, UserInfo>
{
    // short ttl so we don't risk having banned users online for too long
    public GetUserByIdCache() : base(TimeSpan.FromSeconds(15))
    {
        
    }
}
public class GetUserByNameCache : GenericMemoryCache<string, UserInfo>
{
    // short ttl so we don't risk having banned users online for too long
    public GetUserByNameCache() : base(TimeSpan.FromSeconds(15))
    {
        
    }
}
public class UserThemeCache : GenericMemoryCache<long, ThemeTypes>
{
    
}

public class UserAvatarPageStyleCache : GenericMemoryCache<long, AvatarPageStyle>
{
    
}

public class UserConnectionsCache : GenericMemoryCache<long, UserConnections>
{
    
}

public class UserYearCache : GenericMemoryCache<long, WebsiteYear>
{
    public UserYearCache()
    {
        
    }
}