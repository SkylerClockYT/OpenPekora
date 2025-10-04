using Roblox.Libraries.DiscordApi;
using Roblox.Libraries.RobloxApi;
using Roblox.Services;
using Roblox.Services.Games;
using Roblox.Services.PlaceLauncher;
using Roblox.Services.Signer;

namespace Roblox.Website.Controllers
{
    public class ControllerServices
    {
        public AssetsService assets { get; } = new();
        public PromocodesService promocodes { get; } = new();
        public RobloxAssetService robloxAssetCache { get; } = new();
        public UsersService users { get; } = new();
        public AccountInformationService accountInformation { get; } = new();
        public AvatarService avatar { get; } = new();
        public FriendsService friends { get; } = new();
        public GamesService games { get; } = new();
        public PlayerSecurityService playerSecurity { get; } = new();
        public BadgesService badges { get; } = new();
        public GroupsService groups { get; } = new();
        public InventoryService inventory { get; } = new();
        public PrivateMessagesService privateMessages { get; } = new();
        public ThumbnailsService thumbnails { get; } = new();
        public TradesService trades { get; } = new();
        public GameServerService gameServer { get; } = new();
        public SetsService sets { get; } = new();
        public PlaceLauncherService placeLauncher { get; } = new();
        public SignService sign { get; } = new();
        public ForumsService forums { get; } = new();
        public CurrencyExchangeService currencyExchange { get; } = new();
        public AbuseReportService abuseReport { get; } = new();
        public EconomyService economy { get; } = new();
        public CooldownService cooldown { get; } = new();
        public FilterService filter { get; } = new();
        public RobloxApi robloxApi { get; } = new();
        public DiscordBotApi discordBotApi { get; } = new(Configuration.DiscordBotToken);
        public ChatService chat { get; } = new();
    }
}