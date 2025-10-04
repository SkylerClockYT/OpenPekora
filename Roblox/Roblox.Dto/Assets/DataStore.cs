namespace Roblox.Dto.Assets;

public class DataStoreEntry
{
    public long id { get; set; }
    public string key { get; set; }
    public string scope { get; set; }
    public string name { get; set; }
    public string? value { get; set; }
}

public class OrderedDataStoreEntry
{
    public long id { get; set; }
    public string key { get; set; }
    public string name { get; set; }
    public long value { get; set; }
}