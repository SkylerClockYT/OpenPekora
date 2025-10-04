using System.Net;
using System.Text.Json.Serialization;
using Roblox.Models.GameServer;

namespace Roblox.Models.Games;
public class PlaceLaunchRequest
{
    public string? request { get; set; } = "RequestGame";
    public long placeId { get; set; }
    public Guid? gameId { get; set; } = null;
    public bool? isPartyLeader { get; set; } = false;
    public bool? isTeleport { get; set; } = false;
    public string? accessCode { get; set; }
    public string? linkCode { get; set; }
    public string? privateGameMode { get; set; }
    public string? cookie { get; set; }
    public string? username { get; set; }
    public long? userId { get; set; }
    public bool? special { get; set; } = false;
}

public class PlaceLaunchResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? jobId { get; set; }
    public int status { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? joinScriptUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? authenticationUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? authenticationTicket { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public dynamic? settings { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? message { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public dynamic? joinScript { get; set; }
}