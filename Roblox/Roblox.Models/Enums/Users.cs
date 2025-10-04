namespace Roblox.Models.Users
{
    public enum AccountStatus
    {
        Ok = 1,
        Suppressed,
        Deleted,
        Poisoned,
        MustValidateEmail,
        Forgotten,
        MachineBanned,
    }

    public enum MembershipType
    {
        None = 0,
        BuildersClub,
        TurboBuildersClub,
        OutrageousBuildersClub,
        Premium,
    }

    public enum Gender
    {
        Unknown = 1,
        Male = 2,
        Female = 3,
    }

    public enum GeneralPrivacy
    {
        NoOne = 1,
        Friends,
        Following,
        Followers,
        All = 6,
    }

    public enum TradeQualityFilter
    {
        None = 1,
        Low,
        Medium,
        High,
    }

    public enum InventoryPrivacy
    {
        NoOne = 1,
        Friends,
        FriendsAndFollowing,
        FriendsFollowingAndFollowers,
        AllAuthenticatedUsers,
        AllUsers,
    }

    public enum ThemeTypes
    {
        Light = 1,
        Dark,
    }
    
    public enum AvatarPageStyle
    {
        Modern = 1,
        Legacy,
    }

    public enum PresenceType
    {
        Offline = 0,
        Online,
        InGame,
        InStudio,
    }

    public enum PasswordResetState
    {
        Created = 1,
        PasswordChanged = 2,
    }
    
    public class UserConnections
    {
        public string? discord { get; set; }
        public string? twitter { get; set; }
        public string? tiktok { get; set; }
        public string? twitch { get; set; }
        public string? youtube { get; set; }
        public string? telegram { get; set; }
        public string? github { get; set; }
        public string? roblox { get; set; }
    }
}

