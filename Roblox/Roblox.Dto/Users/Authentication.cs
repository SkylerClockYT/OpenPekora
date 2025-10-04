namespace Roblox.Dto.Authentication
{
    public class LoginTicet
    {
        public long userId { get; set; }
        public string challengeId { get; set; }
        public string hashedIp { get; set; }
    }
    public class TwoFactorTicket
    {
        public long userId { get; set; }
        public string hashedIp { get; set; }
    }
    /// <summary>
    /// Error codes for HTTP 400 responses.
    /// </summary>
    public enum LoginError400
    {
        /// <summary>
        /// 0: An unexpected error occurred.
        /// </summary>
        UnexpectedError = 0,

        /// <summary>
        /// 3: Username and Password are required. Please try again.
        /// </summary>
        UsernamePasswordRequired = 3,

        /// <summary>
        /// 8: Login with received credential type is not supported.
        /// </summary>
        CredentialTypeNotSupported = 8
    }

    /// <summary>
    /// Error codes for HTTP 403 responses.
    /// </summary>
    public enum LoginError403
    {
        /// <summary>
        /// 0: Token Validation Failed.
        /// </summary>
        TokenValidationFailed = 0,

        /// <summary>
        /// 1: Incorrect username or password. Please try again.
        /// </summary>
        IncorrectCredentials = 1,

        /// <summary>
        /// 2: You must pass the robot test before logging in.
        /// </summary>
        RobotTestRequired = 2,

        /// <summary>
        /// 4: Account has been locked. Please request a password reset.
        /// </summary>
        AccountLocked = 4,

        /// <summary>
        /// 5: Unable to login. Please use Social Network sign on.
        /// </summary>
        SocialNetworkSignOnRequired = 5,

        /// <summary>
        /// 6: Account issue. Please contact Support.
        /// </summary>
        AccountIssue = 6,

        /// <summary>
        /// 9: Unable to login with provided credentials. Default login is required.
        /// </summary>
        DefaultLoginRequired = 9,

        /// <summary>
        /// 10: Received credentials are unverified.
        /// </summary>
        CredentialsUnverified = 10,

        /// <summary>
        /// 12: Existing login session found. Please log out first.
        /// </summary>
        ExistingLoginSession = 12,

        /// <summary>
        /// 14: The account is unable to log in. Please log in to the LuoBu app.
        /// </summary>
        LuoBuLoginRequired = 14,

        /// <summary>
        /// 15: Too many attempts. Please wait a bit.
        /// </summary>
        TooManyAttempts = 15,

        /// <summary>
        /// 27: The account is unable to login. Please log in with the VNG app.
        /// </summary>
        VngAppLoginRequired = 27
    }

    /// <summary>
    /// Error codes for HTTP 429 responses.
    /// </summary>
    public enum LoginError429
    {
        /// <summary>
        /// 7: Too many attempts. Please wait a bit.
        /// </summary>
        TooManyAttempts = 7
    }

    /// <summary>
    /// Error codes for HTTP 503 responses.
    /// </summary>
    public enum LoginError503
    {
        /// <summary>
        /// 11: Service unavailable. Please try again.
        /// </summary>
        ServiceUnavailable = 11
    }
}