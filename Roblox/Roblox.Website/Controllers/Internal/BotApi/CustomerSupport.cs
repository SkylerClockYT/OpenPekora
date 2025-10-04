using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Users;
using Roblox.Services.Exceptions;
using Roblox.Website.Filters;
using MVC = Microsoft.AspNetCore.Mvc;

namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class CustomerSupport : ControllerBase
    {
        // TODO: Finish this
        // [BotAuthorization]
        // [HttpGetBypass("bot/claimboost")]
        // public async Task<dynamic> ClaimBoost(string discordId)
        // {
        //     var userInfo = await services.users.GetUserByDiscordId(discordId);
        //     //bool isOwned = await services.users.GetUserAssets(userInfo.userId, assetId).Any();
        //     if (true)
        //     {
        //         return new
        //         {
        //             success = false,
        //             message = "You already claimed your booster rewards.",
        //         };
        //     }
        //     return "a";
        // }
        [BotAuthorization]
        [HttpGetBypass("bot/removetwofactor")]
        public async Task<dynamic> RemoveTwoFactor(string discordId, long userId)
        {
            UserInfo userDiscordInfo;
            try
            {
                userDiscordInfo = await services.users.GetUserByDiscordId(discordId);
            }
            // TODO: Make DiscordNotLinkedException
            catch (RecordNotFoundException)
            {
                return new
                {
                    success = false,
                    message = "Your account is not linked, please use the /linkaccount command to link your account",
                };
            }

            // First we check if the user who ran the command if he is a owner if they are not, then throw exception
            if (!StaffFilter.IsOwner(userDiscordInfo.userId))
            {
                return new
                {
                    success = false,
                    message = "You are not authorized to reset passwords",
                };
            }
            await services.users.DeleteTotp(userId);
            return new
            {
                success = true,
                message = "The two-factor authentication has been successfully removed.",
            };
        }
        [BotAuthorization]
        [HttpGetBypass("bot/giveboosteritems")]
        public async Task<dynamic> GiveBoosterItems(string discordId, int robux, long itemId)
        {
            UserInfo userInfo;
            try
            {
                userInfo = await services.users.GetUserByDiscordId(discordId);
            }
            catch (RecordNotFoundException)
            {
                return new
                {
                    success = false,
                    message = "Users account was not linked",
                };
            }
            var doesUserOwnAsset = await services.inventory.IsOwned(userInfo.userId, itemId);
            if (doesUserOwnAsset)
            {
                return new
                {
                    success = false,
                    message = "User already owns booster",
                };
            }
            await services.users.CreateUserAsset(userInfo.userId, itemId);
            await services.economy.IncrementCurrency(userInfo.userId, Models.Economy.CurrencyType.Robux, robux);
            return new
            {
                success = true,
                message = $"Successfully given booster item to {userInfo.username} and added {robux} Robux.",
            };

        }
        [BotAuthorization]
        [HttpGetBypass("bot/resetpassword")]
        public async Task<dynamic> ResetPassword(string discordId, long userId)
        {
            UserInfo userDiscordInfo;
            // Why are we handling this?
            // Because GetUserById/GetUserByDiscordId will throw the RecordNotFound exception and then the message will be "NotFound" and that will be in the embed which we don't want
            try
            {
                userDiscordInfo = await services.users.GetUserByDiscordId(discordId);
            }
            // TODO: Make DiscordNotLinkedException
            catch (RecordNotFoundException)
            {
                return new
                {
                    success = false,
                    message = "Your account is not linked, please use the /linkaccount command to link your account",
                };
            }

            // First we check if the user who ran the command if he is a owner if they are not, then throw exception
            if (!StaffFilter.IsOwner(userDiscordInfo.userId))
            {
                return new 
                {
                    success = false,
                    message = "You are not authorized to reset passwords",
                };
            }
            // This is a extra security check, let's check if the user who we are trying to reset is a staff member if they are then throw a exception
            // if (await StaffFilter.IsStaff(userId))
            // {
            //     return new 
            //     {
            //         success = false,
            //         message = "You are not allowed to reset a password of a staff member",
            //     };
            // }

            // Example: 19bcbfac216d46cbaeb826125d1bae42
            string randomlyGeneratedPassword = (Guid.NewGuid().ToString().Replace("-", "") + Guid.NewGuid().ToString().Replace("-", "")).Substring(0, 32);
            await services.users.UpdatePassword(userId, randomlyGeneratedPassword);
            var userInfo = await services.users.GetUserById(userId);
            return new
            {
                success = true,
                password = randomlyGeneratedPassword,
                message = $"The password has been successfully reset of **{userInfo.username}**.\nThe password has been sent in your DM's"
            };
        }
    }
}
