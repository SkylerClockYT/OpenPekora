namespace Roblox.Website.WebsiteModels.Authentication;
public class LoginRequest
{
    public string? username { get; set; } = null;
    public string ctype { get; set; } = "";
    public string cvalue { get; set; } = "";
    public string password { get; set; } = "";
}

public class LegacyLoginRequest
{
    public string username { get; set; }
    public string password { get; set; }
}
