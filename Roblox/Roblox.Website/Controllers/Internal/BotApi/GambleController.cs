using InfluxDB.Client.Core.Exceptions;
using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Gambling;
using System.Globalization;
using System.Security.Cryptography;
using Roblox.Services.Exceptions;
using Roblox.Dto.Users;
namespace Roblox.Website.Controllers
{
    [MVC.ApiController]
    [MVC.Route("/")]
    public class GambleBot : ControllerBase
    {
        [BotAuthorization]
        [HttpGetBypass("bot/coinflip")]
        public async Task<GamblingResponse> CoinFlip(string discordid, int amount)
        {
            UserInfo userInfo;
            // paranoia check!
            if (amount > 250 || amount < 1)
            {
                return new GamblingResponse
                {
                    message = "You have entered an invalid amount, please enter an amount between 1 and 250",
                    status = (int)GamblingStatus.InvalidAmount
                };
            }

            try
            {
                userInfo = await services.users.GetUserByDiscordId(discordid);
            }
            catch (RecordNotFoundException)
            {
                return new GamblingResponse
                {
                    message = "Your account is not linked, please use the /linkaccount command to link your account",
                    status = (int)GamblingStatus.UserNotFound
                };
            }

            // cooldown is every 2 seconds
            if (!await services.cooldown.TryCooldownCheck($"CoinFlipV1_Cooldown:{userInfo.userId}", TimeSpan.FromSeconds(2)))
            {
                return new GamblingResponse
                {
                    message = "You are on cooldown, please wait a few seconds before gambling again",
                    status = (int)GamblingStatus.UnknownError
                };
            }

            // limit is 20 coinflips per day
            if (!await services.cooldown.TryIncrementBucketCooldown($"CoinFlipV1_Day:{userInfo.userId}", 20, TimeSpan.FromDays(1)))
            {
                return new GamblingResponse
                {
                    message = "You have reached the limit of today, please try again tomorrow",
                    status = (int)GamblingStatus.UnknownError
                };
            }

            var balance = await services.economy.GetUserBalance(userInfo.userId);
            long currentBalance = balance.robux;

            // balance check
            if (currentBalance < amount)
            {
                return new GamblingResponse
                {
                    message = "You do not have enough balance to gamble this amount",
                    status = (int)GamblingStatus.InsufficientBalance
                };
            }

            // 40% chance to win
            int chance = RandomNumberGenerator.GetInt32(1, 100);

            // calculate if win
            bool isWinner = chance <= 40;
            int finalRobux = amount * 2;

            await services.economy.ChargeForCoinflip(userInfo.userId, amount, finalRobux, isWinner);

            if (isWinner)
            {
                return new GamblingResponse
                {
                    message = "You have flipped heads and won!",
                    submessage = $"\nYou have won **{finalRobux}** R$, your balance is updated to **{(currentBalance - amount + finalRobux).ToString("N0", CultureInfo.CurrentCulture)}** R$",
                    status = (int)GamblingStatus.Won,
                };
            }

            return new GamblingResponse
            {
                message = "You have flipped tails and lost",
                submessage = $"\nYou have lost **{amount}** R$, your balance is updated to **{(currentBalance - amount).ToString("N0", CultureInfo.CurrentCulture)}** R$",
                status = (int)GamblingStatus.Lost,
            };
        }
    }
}