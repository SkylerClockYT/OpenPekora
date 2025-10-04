using System;
using System.Collections.Generic;
using System.Linq;
using Roblox.Dto.Assets;

namespace Roblox.Services;


public class GetLatestAssetVersionCache : GenericMemoryCache<long, AssetVersionEntry>
{
    // 2 minutes should be enough
    public GetLatestAssetVersionCache() : base(TimeSpan.FromMinutes(2))
    {

    }
}
