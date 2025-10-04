using Roblox.Models.Assets;
namespace Roblox.Models.Studio;

public class UniversePermissionModel
{
    public string action { get; set; }
    public CreatorType subjectType { get; set; }
    public long subjectId { get; set; }
}
public class UpdatePlace
{
    public long id { get; set; }
    public long universeid { get; set; }
}