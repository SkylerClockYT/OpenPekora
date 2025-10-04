using System.Security.AccessControl;
using Dapper;
using InfluxDB.Client.Api.Domain;
using Microsoft.VisualBasic;
using Roblox.Dto.Assets;
using Roblox.Dto.Persistence;

namespace Roblox.Services;

public enum KeyType
{
    Standard = 1,
    Sorted,
}

public class DataStoreService : ServiceBase, IService
{
    private KeyType ParseType(string? type)
    {
        if (type == "sorted")
            return KeyType.Sorted;

        if (string.IsNullOrWhiteSpace(type) || type == "standard")
            return KeyType.Standard;
        throw new ArgumentException("Invalid " + nameof(type));
    }
    public async Task<IEnumerable<OrderedDataStoreEntry>> GetAllOrderedEntries(long placeId, string key, string scope, bool isAscending, int pageSize, long? inclusiveMinValue = null, long? inclusiveMaxValue = null, string? exclusiveStartKeyRaw = null)
    {
        long? exclusiveStartValue = null;
        string? exclusiveStartKey = null;

        if (exclusiveStartKeyRaw is not null)
        {
            string[] parts = exclusiveStartKeyRaw.Split('$');
            exclusiveStartKey = parts[0];
            // second is the start value
            if (long.TryParse(parts[1], out long result))
            {
                exclusiveStartValue = result;
            }
        }

        var query = new SqlBuilder();
        var template = query.AddTemplate(@"SELECT id, value::BIGINT AS value, name, updated_at FROM asset_datastore /**where**/ /**orderby**/");
        query.Where("asset_id = :placeId AND key = :key AND scope = :scope", new
        {
            placeId,
            key,
            scope
        });
        query.OrderBy($@"value::BIGINT " + (isAscending ? "ASC" : "DESC"));

        if (inclusiveMinValue is not 0)
            query.Where("value::BIGINT >= :inclusiveMinValue", new { inclusiveMinValue });
        if (inclusiveMaxValue is not 0)
            query.Where("value::BIGINT <= :inclusiveMaxValue", new { inclusiveMaxValue });
        query.Where("value::TEXT ~ '^-?[0-9]+$' OR value IS NULL");


        if (exclusiveStartKey != null)
        {
            if (exclusiveStartValue != null)
            {
                var condition = isAscending
                    ? "(key > :exclusiveStartKey OR (key = :exclusiveStartKey AND value::BIGINT > :exclusiveStartValue))"
                    : "(key < :exclusiveStartKey OR (key = :exclusiveStartKey AND value::BIGINT < :exclusiveStartValue))";

                query.Where(condition, new { exclusiveStartKey, exclusiveStartValue });
            }
            else
            {
                var condition = isAscending
                    ? "key > :exclusiveStartKey"
                    : "key < :exclusiveStartKey";

                query.Where(condition, new { exclusiveStartKey });
            }
        }

        string rawSql = template.RawSql;

        return (await db.QueryAsync<OrderedDataStoreEntry>(template.RawSql, template.Parameters)) ?? Enumerable.Empty<OrderedDataStoreEntry>();
    }

    public async Task<IEnumerable<DataStoreEntry>> GetAllEntries(long placeId, string key, string scope, string name)
    {
        var result = await db.QueryAsync<DataStoreEntry>(
            "SELECT id, key, scope, name, value from asset_datastore WHERE asset_id = :place_id AND key = :key AND scope = :scope AND name = :name ORDER BY id DESC LIMIT 250",
            new
            {
                place_id = placeId,
                key,
                scope,
                name,
            });
        return result ?? Enumerable.Empty<DataStoreEntry>();
    }
    public async Task<IEnumerable<DataStoreEntry>> MultiGetDataStores(long placeId, string type, string scope, List<QueuedKeyEntry> keys)
    {
        if (keys.Count == 0)
            return Enumerable.Empty<DataStoreEntry>();

        var query = new SqlBuilder();
        // Really hackys
        var template = query.AddTemplate("SELECT id, scope, key, name, value FROM asset_datastore /**where**/ /**orderby**/ LIMIT 250");
        foreach (var k in keys)
        {
            query.Where("asset_id = :placeId AND key = :key AND scope = :scope AND name = :name", new
            {
                placeId,
                key = k.key,
                scope = k.scope,
                name = k.target,
            });
        }

        return await db.QueryAsync<DataStoreEntry>(template.RawSql, template.Parameters);
    }

    private async Task PurgeExpiredEntries(DataStoreEntry[] all)
    {
        if (all.Length >= 5)
        {
            foreach (var item in all.Skip(5))
            {
                await db.ExecuteAsync(
                    "DELETE FROM asset_datastore WHERE id = :id",
                    new
                    {
                        id = item.id,
                    });
            }
        }
    }

    public async Task Set(long placeId, string key, string type, string scope, string target, int valueLength, string value)
    {
        if (valueLength != value.Length)
            throw new Exception("ValueLength != value.length");
        if (valueLength > 1024 * 1024 * 1)
            throw new Exception("Value length limit exceeded, max 1MB");

        // target is the data store target (e.g. would be "DS" in game:GetService("DataStoreService"):GetDataStore("DS")
        // key is the DS key
        // scope is either global or a custom scope - essentially a key prefix

        //var t = ParseType(type);
        //if (t != KeyType.Standard)
            //return; // ignore for now

        var uni = placeId == 0 ? 0 : await ServiceProvider.GetOrCreate<GamesService>().GetUniverseId(placeId);

        await db.ExecuteAsync("INSERT INTO asset_datastore (asset_id, universe_id, scope, key, name, value) VALUES (:place_id, :universe_id, :scope, :key, :name, :value) ON CONFLICT (asset_id, universe_id, scope, key, name) DO UPDATE SET value = :value", new
        {
            place_id = placeId,
            universe_id = uni,
            scope = scope,
            key = key,
            name = target,
            value = value,
        });
    }
    public async Task Increment(long placeId, string key, string type, string scope, string target, long value)
    {
        await db.ExecuteAsync("UPDATE asset_datastore SET value = CAST(CAST(value AS BIGINT) + :value AS VARCHAR) WHERE asset_id = :place_id AND key = :key AND scope = :scope AND name = :name", new
        {
            place_id = placeId,
            key = key,
            scope = scope,
            name = target,
            value
        });
    }

    public async Task<string?> Get(long placeId, string type, string scope, string key, string target)
    {
        //var t = ParseType(type);
        //if (t != KeyType.Standard)
        //{
            // Ignored
            //return null;
        //}

        // Type can be "standard" or "sorted"
        // long placeId, string type, string scope
        var ent = await GetAllEntries(placeId, key, scope, target);
        if (ent.Any())
        {
            return ent.FirstOrDefault()?.value;
        }
        return null;
    }

    public bool IsThreadSafe()
    {
        return false;
    }

    public bool IsReusable()
    {
        return false;
    }
}