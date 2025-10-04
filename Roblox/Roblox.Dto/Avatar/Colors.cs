using Roblox.Models.Avatar;

namespace Roblox.Dto.Avatar;

public class ColorEntry
{
    public int headColorId { get; set; }
    public int torsoColorId { get; set; }
    public int leftArmColorId { get; set; }
    public int rightArmColorId { get; set; }
    public int leftLegColorId { get; set; }
    public int rightLegColorId { get; set; }

}

public class BodyScales
{
    public double height { get; set; }
    public double width { get; set; }
    public double head { get; set; }
    public double depth { get; set; }
    public double proportion { get; set; }
    public double bodyType { get; set; }
}

public class AvatarWithColors : ColorEntry
{
    public AvatarType avatarType { get; set; }
    public string? thumbnailUrl { get; set; }
    public string? thumbnail3DUrl { get; set; }
    public string? headshotUrl { get; set; }
    public BodyScales scales { get; set; }
}

public class OutfitAvatar : ColorEntry
{ 
    public long userId { get; set; }
    public double height { get; set; } 
    public double width { get; set; } 
    public double head { get; set; } 
    public double depth { get; set; } 
    public double proportion { get; set; } 
    public double bodyType { get; set; } 
    public AvatarType avatarType { get; set; } 
}
