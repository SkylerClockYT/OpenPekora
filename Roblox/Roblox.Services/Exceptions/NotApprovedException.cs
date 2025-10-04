namespace Roblox.Services.Exceptions;

public class NotApprovedException : System.Exception
{
    public NotApprovedException(long assetId) : base("Asset " + assetId + " is not approved")
    {
        
    }
}