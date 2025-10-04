using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roblox.Dto.Assets;
using Roblox.Dto.Persistence;
using Roblox.Logging;
using Roblox.Services;
using Roblox.Services.Exceptions;
using ServiceProvider = Roblox.Services.ServiceProvider;
namespace Roblox.Website.Controllers
{
    [Route("/")]
    public class Datastores : ControllerBase
    {
        [HttpPostBypass("persistence/increment")]
        public async Task<dynamic> IncrementPersistenceAsync(long placeId, string key, string type, string scope, string target, long? value = null)
        {
            // increment?placeId=%i&key=%s&type=%s&scope=%s&target=&value=%i
            if (!isRCC)
                throw new RobloxException(400, 0, "BadRequest");
            var ds = ServiceProvider.GetOrCreate<DataStoreService>();
            if (value == null)
                value = int.Parse(Request.Form["value"][0]!);
            var result = await ds.Get(placeId, key, type, scope, target);

            if (result is null)
                throw new RobloxException(404, 0, "KeyNotFound");

            long oldValue = long.Parse(result);

            await ds.Increment(placeId, key, type, scope, target, value.Value);
            result = await ds.Get(placeId, key, type, scope, target);

            long newValue = long.Parse(result!);

            Writer.Info(LogGroup.DataStoreService, $"Incremented {key} from {oldValue} to {newValue} for placeId {placeId}, scope {scope}, target {target}");
            return new
            {
                data = newValue,
            };
        }

        [HttpPostBypass("persistence/set")]
        public async Task<dynamic> Set(long placeId, string key, string type, string scope, string target, int valueLength)
        {
            if (!isRCC)
                throw new RobloxException(400, 0, "BadRequest");
            var value = Request.Form["value"][0]!;

            if (type is not "standard")
            {
                // Check if the type is valid
                long.Parse(value);
            }

            await ServiceProvider.GetOrCreate<DataStoreService>()
                .Set(placeId, key, type, scope, target, valueLength, value);
            return new
            {
                data = new
                {
                    Value = value,
                    Scope = scope,
                    Key = key,
                    Target = target
                }
            };
        }
        [HttpPostBypass("persistence/getSortedValues")]
        public async Task<dynamic> GetSortedPersistenceValues(long placeId, string type, string scope, string key, bool ascending, int pageSize = 50, long? inclusiveMinValue = 0, long? inclusiveMaxValue = 0, string? exclusiveStartKey = null)
        {
            // persistence/getSortedValues?placeId=0&type=sorted&scope=global&key=Level%5FHighscores20&pageSize=10&ascending=False"
            // persistence/set?placeId=124921244&key=BF2%5Fds%5Ftest&&type=standard&scope=global&target=BF2%5Fds%5Fkey%5Ftmp&valueLength=31
            using var ds = ServiceProvider.GetOrCreate<DataStoreService>();
            if (!isRCC)
                throw new RobloxException(403, 0, "BadRequest");
            if (pageSize > 100)
                throw new RobloxException(400, 0, "PageSizeTooLarge");
            if (type != "sorted")
                throw new RobloxException(400, 0, "TypeNotSorted");

            var result = await ds.GetAllOrderedEntries(placeId, key, scope, ascending, pageSize, inclusiveMinValue, inclusiveMaxValue, exclusiveStartKey);
            // Let's check if there are possibily more results
            if (result.Count() >= pageSize)
            {
                // Get the last result
                var lastResult = result.LastOrDefault();
                exclusiveStartKey = $"{lastResult!.name}${lastResult!.value}";
            }
            return new
            {
                data = new
                {
                    Entries = result.Select(c => new GetKeyEntrySorted
                    {
                        Target = c.name,
                        Value = c.value,
                    }),
                    ExclusiveStartKey = exclusiveStartKey
                },
            };
        }

        [Consumes("application/x-www-form-urlencoded")]
        [HttpPostBypass("persistence/getv2")]
        public async Task<dynamic> GetPersistenceV2(long placeId, string type, string scope, [FromForm] QueuedKeysRequest request)
        {
            if (!isRCC)
                throw new RobloxException(403, 0, "Unauthorized");
            
            using var ds = ServiceProvider.GetOrCreate<DataStoreService>();

            var result = await ds.MultiGetDataStores(placeId, type, scope, request.qkeys);
            return new
            {
                data = result.Select(c => new GetKeyEntry
                {
                    Key = c.key,
                    Scope = c.scope,
                    Target = c.name,
                    Value = type != "standard" ? Convert.ToInt64(c.value) : c.value!
                })
            };

            //dynamic result = new List<GetKeyEntry>();
            //bool isEmpty = false;
            //string qKeyscope;
            //string qKeyTarget;
            //string qKeyKey;
            //while (true)
            //{
            //    qKeyscope = Request.Form[$"qkeys[{countRequest}].scope"]!;
            //    qKeyTarget = Request.Form[$"qkeys[{countRequest}].target"]!;
            //    qKeyKey = Request.Form[$"qkeys[{countRequest}].key"]!;

            //    if (qKeyscope == null || qKeyTarget == null || qKeyKey == null)
            //        break;

            //    var entry = await ds.GetAllEntries(placeId, qKeyKey, scope, qKeyTarget);
            //    if (entry == null)
            //    {
            //        countRequest++;
            //        continue;
            //    }
            //    foreach (DataStoreEntry item in entry)
            //    {
            //        //should never be null
            //        if (String.IsNullOrEmpty(item.value))
            //        {
            //            isEmpty = true;
            //            break;
            //        }
            //        result.Add(new GetKeyEntry()
            //        {
            //            Key = qKeyKey,
            //            Scope = qKeyscope,
            //            Target = qKeyTarget,
            //            Value = item.value
            //        });
            //    }
            //    countRequest++;
            //}
            //if (isEmpty)
            //    result = new List<string>();
            //GetKeysRequest
            //            var finalData = new { data = result};
            //string jsonString = JsonConvert.SerializeObject(finalData);
            //return new
            //{

            //}
        }
    }
}
