using Roblox.Dto.Games;

namespace Roblox.Services;
public class GetGamesListCache : GenericMemoryCache<string, IEnumerable<GameListEntry>>
{
    public GetGamesListCache() : base(TimeSpan.FromSeconds(15))
    {

    }
}