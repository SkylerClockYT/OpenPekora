using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Roblox.Logging;
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Roblox.Libraries.RobloxApi;
public class ProductDataResponse
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Models.Assets.Type? AssetTypeId { get; set; }
    public bool? IsLimited { get; set; }
    public bool? IsLimitedUnique { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Updated { get; set; }
}

public class AssetDelivery
{
    public string location { get; set; }
    public string requestId { get; set; }
    public Models.Assets.Type assetTypeId { get; set; }
}

public class AssetDeliveryEntry
{
    public string? assetFormat { get; set; }
    public string? location { get; set; }
}

public class AssetDeliveryV2Response
{
    public Models.Assets.Type assetTypeId { get; set; }
    public IEnumerable<AssetDeliveryEntry>? locations { get; set; }
}

public class AssetDeliveryV1BatchResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? location { get; set; }
    public string requestId { get; set; }
    public bool IsHashDynamic { get; set; }
    public bool IsCopyrightProtected { get; set; }
    public bool isArchived { get; set; }
    public int assetTypeId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AssetDeliveryV1Error>? errors { get; set; }
}

public class AssetDeliveryV1Error
{
    public int code { get; set; }
    public string message { get; set; }
}



public class ProductInfoWithAssetDelivery : ProductDataResponse
{
    public string? location { get; set; }
}

public class UsersResponseV1
{
    public string? description { get; set; }
    public string? created { get; set; }
}

public class MultiGetByUsernameRequest 
{
    public List<string> usernames { get; set; }
    public bool excludeBannedUsers { get; set; }
}


public class MultiGetByUsernameResponseEntry 
{
    public string requestedUsername { get; set; }
    public bool hasVerifiedBadge { get; set; }
    public long id { get; set; }
    public string name { get; set; }
    public string displayName { get; set; }
}

public class MultiGetByUsernameResponse
{
    public List<MultiGetByUsernameResponseEntry> data { get; set; }
}

public class AssetTypeEntry
{
    public int id { get; set; }
    public string name { get; set; }
}

public class AvatarAsset
{
    public long id { get; set; }
    public AssetTypeEntry assetType { get; set; }
}

public class AvatarResponse
{
    public IEnumerable<AvatarAsset> assets { get; set; }
}

public class MultiGetDetailsRequestEntry
{
    public string itemType { get; set; }
    public long id { get; set; }
}

public class MultiGetDetailsRequest
{
    public IEnumerable<MultiGetDetailsRequestEntry> items { get; set; }
}

public class MultiGetDetailsResponseEntry
{
    public long id { get; set; }
    public string itemType { get; set; }
    public int price { get; set; }
    public string creatorType { get; set; }
    public long creatorTargetId { get; set; }
}

public class MultiGetDetailsResponse
{
    public IEnumerable<MultiGetDetailsResponseEntry> data { get; set; }
}

public class CountResponse
{
    public long count { get; set; }
}

public class InventoryEntry
{
    public long assetId { get; set; }
}

public class InventoryResponse
{
    public IEnumerable<InventoryEntry> data { get; set; }
}

public class InvalidUserException : Exception
{
    public InvalidUserException() {}
}

public class BadgeEntry
{
    public int id { get; set; }
}

public class ProfileHeaderResponse
{
    [JsonPropertyName("IsVieweePremiumOnlyUser")]
    public bool isPremium { get; set; }
    [JsonPropertyName("ProfileDisplayName")]
    public string displayName { get; set; }
    [JsonPropertyName("PreviousUserNames")]
    public string previousUsernames { get; set; }
}

public class BundleItemEntry
{
    public long id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
}

public class BundleProduct
{
    public long? priceInRobux { get; set; }
}

public class BundleResponseEntry
{
    public long id { get; set; }
    public string name { get; set; }
    public string description { get; set; }

    public string bundleType { get; set; }
    public List<BundleItemEntry> items { get; set; }
    public BundleProduct product { get; set; }
}

public class MultiGetBundlesResponse
{
    public List<BundleResponseEntry> data { get; set; }
}
public class BatchAssetRequest
{
    public long assetId { get; set; }
    public string assetType { get; set; }
    public string requestId { get; set; }
}

public class RobloxApi
{
    private static HttpClient _client { get; } = new(new HttpClientHandler()
    {
        AutomaticDecompression = DecompressionMethods.All,
    });
    private class RobloxApiHttpClient : HttpClient
    {
        public RobloxApiHttpClient() : base(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            DefaultRequestHeaders.Add("Cookie", ".ROBLOSECURITY=" + "_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_B98A878CD86A739316DFF0002B47047D8E4E4CD85F5A1B66F638AABC0E61062254A32AD844760474A71608043B11214797473137A6A1211B9DE70226929C97AA9C810BB57238BFF9EA81BD4BDBA5FDF6F53B785F361228DAEB060025C250CE80FB595C2E6E41C92F9C60B540CE23499B405C5A1DDF64562A6AA47F778CFD75CD10AE4A4FB542881ED5936006D00A918D2963F0D1C08708E7CC49EEB842BAF70CAC420F05C33AE0ED7503B637FAA2E6EBAB7110E14C16CEAB5BFF40AC3E21B59E7C297E6190CFC595680F40E7AE25BAAFAFFDDB6098B44F686681A6C933712824432703133A79440AF00CEF08C9338530C347D9A1CBCAF301A1609D2F63A71FE8A634C2CD66879A97F1A53BF4A8E731DE81E1887CC5ED2405DCE88B2658D64D02BA573A5C763F4AFEB60FEC48A822C691C23FF9345E7B7CE51503F591D3E102CB37031F2C826345A59C14517B1B2C011E30279B9457CCFCFCE1D2571E3B6AFD8DC457B0D5767E409CE9A3A43E2C912A2D672AFC6E73B1215B2EF3AFDE32F874BC2FE6B7141A64346F4674748014056AAC0F014D03673A3574DB3CAA75C35DCF6C798F1A0E951E43DEF153B6DFB07F56A719372FECEF63F0E1726DC15355664995978372BF32E64675BBA32A15738BBD5FBA857A4FEB7EAFB423E255CCAE9B7ABDFF869F79C3FA288B40CAB12EF9162D92BC128F9D997125EB61ECDD0A192F5A752D260147C3590FF5F0F5E506F736BF6A209A879166243D92F1F7BEF01427F01C3E83CA91A1C9AD03D04127EF56A9CC1D91C37D0EE1DA1B0785EA20DADA29860708EC741378198EE7AA171762AC9969A0B6371D22B925487660181ACFDE13E5725BD4ACBB8C47F54AA1D90F61268CF12F0D44E86CE02454C2344EC73FD8D1AFCEE09FA4CCC97FF2039B3313563DF502C700D814BF4B62A668FF94155CBECE249ED6076E3D934CF473C30ABDAF9FE72DAEDA8BD439A055CAE8003A80F64F9EB3099C0CBEF18FCB93675A404D0E6B835275EBBFC4638FBC7EF559AA5DD68CDDAEBFDED3F11B056E72CC90ECD28733D80CB0D12633A14065AC3114D5EA98F6B4B03B033F3936AD2F349D38ABCFA74A02C8705098349940C77CF570FFD6BA98817DF18B79531B");
            DefaultRequestHeaders.Add("Accept", "*/*");
            DefaultRequestHeaders.Add("User-Agent", "Roblox/WinInet");
            DefaultRequestHeaders.Add("Roblox-Browser-Asset-Request", "false");
        }
    }


    private static HttpClient robloxApiClient { get; } = new RobloxApiHttpClient();
    public async Task<ProductInfoWithAssetDelivery> GetProductInfoAssetDelivery(long assetId)
    {
        // Literally all it gets is the "assetTypeId". Everything else is blank.
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(TimeSpan.FromSeconds(30));
        var response = await robloxApiClient.GetAsync("https://assetdelivery.roblox.com/v2/asset?id=" + assetId, cancel.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Unexpected status code from AssetDeliveryV2: " + response.StatusCode);
        }

        var str = await response.Content.ReadAsStringAsync(cancel.Token);
        var json = JsonSerializer.Deserialize<AssetDeliveryV2Response>(str);
        if (json == null)
            throw new Exception("Bad json from assetdelivery");
        return new ProductInfoWithAssetDelivery()
        {
            AssetTypeId = json.assetTypeId,
            Name = "Asset" + assetId,
            Description = "ConversionV1.0",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,

            location = json.locations?.FirstOrDefault(a => a.assetFormat == "source")?.location,
        };
    }

    private async Task<ProductDataResponse> GetProductInfoFromHtml(long assetId)
    {
        // why? well one reason: rate limits.
        // roblox heavily rate limits productinfo for some reason, but html doesn't seem as bad
        var watch = new Stopwatch();
        watch.Start();
        const int maxAttemptsMs = 60000;
        while (watch.ElapsedMilliseconds < maxAttemptsMs)
        {
            using var cancel = new CancellationTokenSource();
            cancel.CancelAfter(TimeSpan.FromMilliseconds(maxAttemptsMs));
            var url = $"https://www.roblox.com/catalog/{assetId}/-";
            var response = await _client.GetAsync(url, cancel.Token);
            if (!response.IsSuccessStatusCode)
            {
                Writer.Info(LogGroup.RealRobloxApi, "GetProductInfoHtml failed - " + response.StatusCode);
                await Task.Delay(TimeSpan.FromSeconds(2));
                continue;
            }

            var str = await response.Content.ReadAsStringAsync(cancel.Token);
            // regex
            var itemName = System.Web.HttpUtility.HtmlDecode(new Regex("data-item-name=\"(.+)\"").Match(str).Groups[1].Value);
            var assetTypeId = Enum.Parse<Models.Assets.Type>(new Regex("data-asset-type-id=\"(.+)\"").Match(str).Groups[1].Value);
            if (!Enum.IsDefined(assetTypeId))
            {
                throw new Exception("Invalid assetTypeId: " + assetTypeId);
            }
            // no way to get some values like created :(
            // we can technically get the description with regex but that might break easily
            return new ProductDataResponse()
            {
                Created = DateTime.UtcNow,
                AssetTypeId = assetTypeId,
                Description = "",
                Name = itemName,
                Updated = DateTime.UtcNow,
            };

        }

        throw new Exception("Timeout getting details from html");
    }

    public async Task<UsersResponseV1> GetUserInfo(long userId)
    {
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(TimeSpan.FromSeconds(5));

        var result = await _client.GetAsync("https://users.roblox.com/v1/users/" + userId, cancel.Token);
        if (!result.IsSuccessStatusCode)
        {
            throw new Exception("Unexpected response from Roblox: " + result.StatusCode);
        }

        var str = await result.Content.ReadAsStringAsync(cancel.Token);
        var json = JsonSerializer.Deserialize<UsersResponseV1>(str);
        if (json == null)
            throw new Exception("Null json returned from users api");
        return json;
    }

    public async Task<long> GetUserIdByUsername(string userName) 
    {
        MultiGetByUsernameRequest request = new()
        {
            usernames = new(),
            excludeBannedUsers = false,
        };
        request.usernames.Add(userName);
        var result = await _client.PostAsync("https://users.roblox.com/v1/usernames/users", new StringContent(JsonSerializer.Serialize(request)));
        if (result == null)
            throw new Exception("Null response from Roblox");
        if (!result.IsSuccessStatusCode)
            throw new Exception("Unexpected response from Roblox: " + result.StatusCode);
        var response = await result.Content.ReadFromJsonAsync<MultiGetByUsernameResponse>(); 
        if (response == null)
            throw new Exception("Null response from users api");
        if(response.data.Count == 0)
            throw new Exception("User not found");

        return response.data.FirstOrDefault()!.id;
    }

    public async Task<bool> DoesUserOwnAsset(long userId, long assetId)
    {
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(TimeSpan.FromSeconds(5));

        var result =
            await _client.GetAsync(
                "https://inventory.roblox.com/v1/users/" + userId + "/items/Asset/" + assetId + "/is-owned",
                cancel.Token);
        if (!result.IsSuccessStatusCode)
            throw new Exception("Unexpected response: " + result.StatusCode);

        var str = await result.Content.ReadAsStringAsync(cancel.Token);
        return str switch
        {
            "true" => true,
            "false" => false,
            _ => throw new Exception("Unexpected response body: " + str)
        };
    }

    public async Task<ProductDataResponse> GetProductInfo(long assetId, bool allFieldsRequired = false)
    {
        var watch = new Stopwatch();
        watch.Start();
        const int maxAttemptTimeMs = 5000;

        while (watch.ElapsedMilliseconds < maxAttemptTimeMs)
        {
            try
            {
                using var cancel = new CancellationTokenSource();
                cancel.CancelAfter(TimeSpan.FromMilliseconds(maxAttemptTimeMs));
                var url = $"https://economy.roproxy.com/v2/assets/{assetId}/details";
                var result = await _client.GetAsync(url, cancel.Token);
                if (result.StatusCode is HttpStatusCode.TooManyRequests)
                {
                    Writer.Info(LogGroup.RealRobloxApi, "conversion error - got 429 during getproductinfo");
                    if (allFieldsRequired)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancel.Token);
                        continue;
                    }
                    break; // switch to html
                }
                if (!result.IsSuccessStatusCode)
                    throw new Exception("Unexpected response from Roblox: " + result.StatusCode + " (URL=" + url + ")");
                var str = await result.Content.ReadAsStringAsync(cancel.Token);
                var des = JsonSerializer.Deserialize<ProductDataResponse>(str);
                if (des == null)
                    throw new Exception("Null product data response from Roblox");
                return des;
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
        // last attempt
        return await GetProductInfoFromHtml(assetId);
    }

    public async Task<Stream> GetStreamAsync(string url)
    {
        var strResult = await robloxApiClient.GetAsync(url);
        if (!strResult.IsSuccessStatusCode)
            throw new Exception("Bad response in GetStreamAsync: " + strResult.StatusCode);
        return await strResult.Content.ReadAsStreamAsync();
    }

    public static async Task<IEnumerable<AssetDeliveryV1BatchResponse>> GetAssetsFromBatch(List<BatchAssetRequest> assets, long? placeId = 1818)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, 
            "https://assetdelivery.roblox.com/v1/assets/batch");

        request.Content = new StringContent(JsonSerializer.Serialize(assets), Encoding.UTF8, "application/json");
        request.Headers.Add("Roblox-Place-Id", placeId.ToString());

        var result = await robloxApiClient.SendAsync(request);
        if (!result.IsSuccessStatusCode)
            throw new Exception("Unexpected response from Roblox: " + result.StatusCode);

        var response = await result.Content.ReadFromJsonAsync<IEnumerable<AssetDeliveryV1BatchResponse>>();
        if (response == null)
            throw new Exception("Null response from batch request");

        return response;
    }
    public static async Task<AssetDelivery?> GetAssetById(long assetId, long? placeId = 1818)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://assetdelivery.roblox.com/v1/assetid/{assetId}");
        request.Headers.Add("Roblox-Place-Id", placeId.ToString());
        var result = await robloxApiClient.SendAsync(request);

        if (!result.IsSuccessStatusCode)
            return null;

        var body = await result.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<AssetDelivery>(body)!;
    }
    public async Task<Stream> GetAssetContentFromProxy(long assetId, long? version = null)
    {
        HttpResponseMessage? result = null;
        if (version != null)
        {
            result = await robloxApiClient.GetAsync($"https://assetdelivery.roblox.com/v1/asset?id={assetId}&version={version}");
        }
        else
        {
            result = await robloxApiClient.GetAsync($"https://assetdelivery.roblox.com/v1/asset?id={assetId}");
        }
        if (!result.IsSuccessStatusCode)
            throw new Exception("Unexpected response from Roblox: " + result.StatusCode);
        if (result == null)
            throw new Exception("Null response from Roblox");
        return result.Content.ReadAsStream();
    }
    // hacky
    public async Task<string> GetToolbox(string type, string? keyword,string sortType, int? limit = 30)
    {
        var request = await robloxApiClient.GetAsync($"https://apis.roblox.com/toolbox-service/v1/{type}?keyword={keyword}&sortType={sortType}&limit={limit}");
        return await request.Content.ReadAsStringAsync();
    }

    public async Task<Stream> GetAssetContent(long assetId)
    {
        while (true)
        {
            var result = await _client.GetAsync($"https://assetdelivery.roblox.com/v1/asset?id={assetId}");
            if (result.StatusCode is HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                continue;
            }
            if (!result.IsSuccessStatusCode)
                throw new Exception("Unexpected response from Roblox: " + result.StatusCode);
            var str = await result.Content.ReadAsStringAsync();
            var bod = JsonSerializer.Deserialize<AssetDelivery>(str);
            if (bod == null)
                throw new Exception("Null " + nameof(AssetDelivery) + " from Roblox");
            if (string.IsNullOrEmpty(bod.location))
                throw new Exception("Roblox did not give a URL for this asset content. Is the URL valid?");

            var strResult = await _client.GetAsync(bod.location);
            return await strResult.Content.ReadAsStreamAsync();
        }
    }

    private static Regex assetMatchUrlRegex = new Regex("data-mediathumb-url=\"(.+?)\"");

    public async Task<Stream> GetAssetAudioContent(long assetId)
    {
        var result = await _client.GetAsync($"https://www.roblox.com/library/{assetId}/--");
        if (!result.IsSuccessStatusCode)
            throw new Exception("Asset error: " + result.StatusCode);
        var bod = await result.Content.ReadAsStringAsync();
        var match = assetMatchUrlRegex.Match(bod);
        if (!match.Success)
            throw new Exception("Audio URL match failed for assetid = " + assetId);
        var groups = match.Groups.Values.ToArray();
        if (groups.Length < 1)
            throw new Exception("No match groups for audio URL");

        var fileUrl = groups[1].Value;

        var strResult = await _client.GetAsync(fileUrl);
        return await strResult.Content.ReadAsStreamAsync();
    }

    public async Task<AvatarResponse> GetAvatar(long userId)
    {
        var result = await _client.GetAsync("https://avatar.roblox.com/v1/users/" + userId + "/avatar");
        if (!result.IsSuccessStatusCode)
            throw new Exception("Avatar error: " + result.StatusCode);
        var body = await result.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<AvatarResponse>(body);
        if (json == null)
            throw new Exception("Null avatar response from Roblox");
        return json;
    }

    private string _csrf { get; set; } = "";

    public async Task<MultiGetDetailsResponse> MultiGetAssetDetails(IEnumerable<MultiGetDetailsRequestEntry> request)
    {
        var attempts = 0;
        var s = JsonSerializer.Serialize(new MultiGetDetailsRequest()
        {
            items = request,
        });
        while (true)
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "https://catalog.roblox.com/v1/catalog/items/details");
            msg.Content = new StringContent(s, Encoding.UTF8, "application/json");
            msg.Headers.Add("x-csrf-token", _csrf);

            var result = await _client.SendAsync(msg);
            if (result.StatusCode == HttpStatusCode.Forbidden && result.Headers.Contains("x-csrf-token"))
            {
                Writer.Info(LogGroup.RealRobloxApi, "use new csrf {0}", result.Headers.GetValues("x-csrf-token"));
                _csrf = result.Headers.GetValues("x-csrf-token").First();
                if (attempts > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempts));
                }
                attempts++;
                continue;
            }
            var body = await result.Content.ReadAsStringAsync();

            if (!result.IsSuccessStatusCode)
                throw new Exception("Get asset details request error: " + result.StatusCode + "\n" + body);
            var json = JsonSerializer.Deserialize<MultiGetDetailsResponse>(body);
            if (json == null)
                throw new Exception("Null multi-get response from Roblox");
            return json;
        }
    }

    public async Task<long> CountFollowers(long userId)
    {
        var result = await _client.GetAsync("https://friends.roblox.com/v1/users/"+userId+"/followers/count");
        if (!result.IsSuccessStatusCode)
            throw new Exception("Follower count error: " + result.StatusCode);
        var body = await result.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<CountResponse>(body);
        if (json == null)
            throw new Exception("Null follower count response from Roblox");
        return json.count;
    }

    public async Task<long> CountFriends(long userId)
    {
        var result = await _client.GetAsync("https://friends.roblox.com/v1/users/"+userId+"/friends/count");
        if (!result.IsSuccessStatusCode)
            throw new Exception("Friends count error: " + result.StatusCode);
        var body = await result.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<CountResponse>(body);
        if (json == null)
            throw new Exception("Null friends count response from Roblox");
        return json.count;
    }

    public async Task<InventoryResponse> GetInventory(long userId, string? cursor = null)
    {
        var url = "https://inventory.roblox.com/v2/users/"+userId+"/inventory?assetTypes=Hat%2CGear%2CHairAccessory%2CNeckAccessory%2CShoulderAccessory%2CBackAccessory%2CFrontAccessory%2CWaistAccessory&limit=100&sortOrder=Asc&cursor=" + (cursor ?? "");
        var result = await _client.GetAsync(url);
        if (result.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
            throw new InvalidUserException();

        if (!result.IsSuccessStatusCode)
            throw new Exception("Inventory error: " + result.StatusCode);
        var body = await result.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<InventoryResponse>(body);
        if (json == null)
            throw new Exception("Null inventory response from Roblox");
        return json;
    }

    public async Task<IEnumerable<BadgeEntry>> GetRobloxBadges(long userId)
    {
            var result = await _client.GetAsync("https://accountinformation.roblox.com/v1/users/"+userId+"/roblox-badges");
        if (!result.IsSuccessStatusCode)
            throw new Exception("Badges error: " + result.StatusCode);
        var body = await result.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<IEnumerable<BadgeEntry>>(body);
        if (json == null)
            throw new Exception("Null badges response from Roblox");
        return json;
    }

    public async Task<ProfileHeaderResponse> GetProfile(long userId)
    {
        var url = "https://www.roblox.com/users/profile/profileheader-json?userid=" + userId;
        var result = await _client.GetAsync(url);
        if (!result.IsSuccessStatusCode)
            throw new Exception("Premium error: " + result.StatusCode);
        var body = await result.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<ProfileHeaderResponse>(body);
        if (json == null)
            throw new Exception("Null profile response from Roblox");
        return json;
    }

    public async Task<BundleResponseEntry> GetBundle(long bundleId)
    {
        var url = "https://catalog.roblox.com/v1/bundles/details?bundleIds=" + bundleId; // MultiGetBundlesResponse
        var result = await _client.GetAsync(url);
        if (!result.IsSuccessStatusCode)
            throw new Exception("GetBundle error: " + bundleId + " " + result.StatusCode);
        var json = JsonSerializer.Deserialize<List<BundleResponseEntry>>(await result.Content.ReadAsStringAsync());
        if (json == null)
            throw new Exception("Null bundle response");
        if (json.Count != 1)
            throw new Exception("No bundle matches this id");
        return json[0];
    }

}