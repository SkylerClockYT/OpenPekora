using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Dto.Avatar;
using Roblox.Exceptions.Services.Users;
using Roblox.Models.Avatar;
using Roblox.Website.Controllers;
using ControllerBase = Roblox.Website.Controllers.ControllerBase;
using Roblox.Dto.Users;
namespace Roblox.Website.Pages.Auth;

public class AccountDeletion : RobloxPageModel
{
    static AccountDeletion()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                var currentTime = DateTime.UtcNow;
                var reset = currentTime.Add(TimeSpan.FromDays(1));
                var trueResetTime = new DateTime(reset.Year, reset.Month, reset.Day, 0, 0, 0);
                if (trueResetTime < currentTime)
                {
                    trueResetTime = trueResetTime.Add(TimeSpan.FromDays(1));
                }
                var delay = trueResetTime.Subtract(currentTime);
                Console.WriteLine("[info] will clear AccountDeletion request dictionary in {0}",delay);
                await Task.Delay(delay);
                attempts.Clear();
            }
        });
    }
    private static Dictionary<string, int> attempts = new();
    public string? successMessage { get; set; }
    public string? failureMessage { get; set; }
    public void OnGet()
    {
        
    }
    
    [BindProperty]
    public string? username { get; set; }
    [BindProperty]
    public string? password { get; set; }
    [BindProperty]
    public string? totpcode { get; set; }


    public async Task<IActionResult> OnPost()
    {
        var services = new ControllerServices();
        var rlKey = ControllerBase.GetIP(ControllerBase.GetRequesterIpRaw(HttpContext)) + "_" + DateTime.UtcNow.ToString("d");
        var tries = attempts.ContainsKey(rlKey) ? attempts[rlKey] : 0;
        if (tries >= 10)
        {
            failureMessage = "You have been making too many attempts. Try again tomorrow.";
            return new PageResult();
        }

        if (!attempts.ContainsKey(rlKey))
            attempts[rlKey] = 0;
        attempts[rlKey]++;
        if (username == null)
        {
            password = null;
            failureMessage = "Invalid username provided.";
            return new PageResult();
        }

        if (password == null)
        {
            password = null;
            failureMessage = "Invalid password provided.";
            return new PageResult();
        }

        UserInfo user;
        try
        {
            user = await services.users.GetUserByName(username);
        }
        catch (UserNotFoundException)
        {
            username = null;
            password = null;
            failureMessage = "The username provided is invalid or does not exist. Your account may already be deleted.";
            return new PageResult();
        }

        var loginOk = await services.users.VerifyPassword(user.userId, password);
        if (!loginOk)
        {
            username = null;
            password = null;
            failureMessage = "The username and password combination provided is invalid. Please try again.";
            return new PageResult();
        }

        if (isPasswordLeaked)
        {
            username = null;
            password = null;
            failureMessage = "The password you entered has been found in a data breach. Please change your password.";
            await services.users.NullifyPassword(user.userId);
            return new PageResult();
        }
        

        if (await services.users.GetTotpStatus(user.userId) == TotpStatus.Enabled)
        {
            TotpInfo? totpInfo = await services.users.GetTotp(user.userId);
            // blank check
            if (string.IsNullOrWhiteSpace(totpcode))
            {
                failureMessage = "You must enter a 2FA Code";
                return new PageResult();
            }
            //try to parse as an long so know its only numbers and not some other garbage
            if (!long.TryParse(totpcode, out var code))
            {
                failureMessage = "The 2FA code you entered is not valid";
                return new PageResult();
            }
            //and as final verify the totp code
            if (!services.users.VerifyTotp(totpInfo.secret, totpcode))
            {
                failureMessage = "The 2FA code you entered is not valid";
                return new PageResult();
            }
        }

        try
        {
            await services.users.DeleteUser(user.userId, false);
        }
        catch (AccountLastOnlineTooRecentlyException)
        {
            username = null;
            password = null;
            failureMessage = "Your account was last online too recently. Please wait a day.";
            return new PageResult();
        }



        // reset av
        await services.avatar.RedrawAvatar(user.userId, new List<long>(), new ColorEntry()
        {
            headColorId = 194,
            torsoColorId = 23,
            rightArmColorId = 194,
            leftArmColorId = 194,
            rightLegColorId = 102,
            leftLegColorId = 102,
        }, AvatarType.R6);
        successMessage = "Your account has been successfully deleted.";
        return new PageResult();
    }
}