namespace Roblox.Website.WebsiteModels.Authentication;

public class TwoFactor
{
    public string username { get; set; }
    public string ticket { get; set; }
    public string code { get; set; }
}

public class TwoFactorEmail
{
    public string challengeId { get; set; }
    public string? verificationToken { get; set; }
    public string? code { get; set; }
}

public class TwoFactorEmailLogin
{
    public string challengeId { get; set; }
    public string verificationToken { get; set; }
    public bool rememberDevice { get; set; }
}

public class TwoFactorLegacy
{
    public string username { get; set; }
    public string tl { get; set; }
    public string identificationCode { get; set; }
}
