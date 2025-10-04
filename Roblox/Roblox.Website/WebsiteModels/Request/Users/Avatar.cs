using Roblox.Dto.Avatar;
using Roblox.Models.Avatar;

namespace Roblox.Website.WebsiteModels;

public class SetWearingAssetsRequest
{
    public IEnumerable<long> assetIds { get; set; }
}

public class SetBodyAttributesRequest
{
    public BodyScales scales { get; set; } 
}

public class SetColorsRequest : Roblox.Dto.Avatar.ColorEntry
{
    
}

public class SetAvatarTypeRequest
{
    public AvatarType playerAvatarType { get; set; }
}

public class CreateOutfitRequest
{
    public string name { get; set; }
}

public class UpdateOutfitRequest
{
    public string? name { get; set; }
}