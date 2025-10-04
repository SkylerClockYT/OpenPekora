using MVC = Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Roblox.Services.App.FeatureFlags;
using BadRequestException = Roblox.Exceptions.BadRequestException;
namespace Roblox.Website.Controllers
{
    [MVC.ApiController]
    [MVC.Route("/")]
    public class Followers: ControllerBase
    {
        [HttpPostBypass("user/follow")]
        public async Task<dynamic> FollowUserLegacy([FromForm] FollowerRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FollowingEnabled);
            if (request.followedUserId == safeUserSession.userId)
                throw new BadRequestException();

            await services.friends.FollowerUser(safeUserSession.userId, request.followedUserId);

            return new
            {
                success = true,
                isCaptchaRequired = false,
            };
        }

        [HttpPostBypass("user/unfollow")]
        public async Task<dynamic> DeleteFollowingLegacy([FromForm] FollowerRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FollowingEnabled);
            await services.friends.DeleteFollowing(safeUserSession.userId, request.followedUserId);
            return new
            {
                success = true,
                isCaptchaRequired = false,
            };
        }

        [HttpPostBypass("user/following-exists")]
        [HttpGetBypass("user/following-exists")]
        public async Task<dynamic> FollowingExists(long userId, long followerUserId)
        {
            return new
            {
                success = true,
                isFollowing = await services.friends.IsOneFollowingTwo(followerUserId, userId),
            };
        }

        [HttpPostBypass("user/multi-following-exists")]
        public async Task<dynamic> MultiGetFollowingExists([FromBody] FilterSocialRequest request)
        {
            var followingDetails = new List<dynamic>();

            foreach (long userId in request.otherUserIds)
            {
                followingDetails.Add(new
                {
                    UserId1 = request.userId,
                    UserId2 = userId,
                    User1FollowsUser2 = await services.friends.IsOneFollowingTwo(request.userId, userId),
                    User2FollowsUser1 = await services.friends.IsOneFollowingTwo(userId, request.userId)
                });
            }

            return new 
            { 
                FollowingDetails = followingDetails 
            };
        }
    }
}