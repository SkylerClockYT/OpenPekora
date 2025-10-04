using MVC = Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Xml;
using Roblox.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Roblox.Services.App.FeatureFlags;
using BadRequestException = Roblox.Exceptions.BadRequestException;
using ServiceProvider = Roblox.Services.ServiceProvider;

using Roblox.Dto.Marketplace;
using Newtonsoft.Json;
using System.Dynamic;
using Roblox.Models;
using Roblox.Dto.Friends;
namespace Roblox.Website.Controllers
{
    public class FollowerRequest
    {
        public long followedUserId { get; set; }
    }
    public class FriendRequest
    {
        public long? requesterUserId { get; set; }
        public long? recipientUserId { get; set; }
        public long? friendUserId { get; set; }
    }
    public class FilterSocialRequest
    {
        public long userId { get; set; }
        public List<long> otherUserIds { get; set; }
    }

    [MVC.ApiController]
    [MVC.Route("/")]
    public class Friends: ControllerBase
    {
        [HttpPostBypass("my/friendsonline")]
        [HttpGetBypass("my/friendsonline")]
        public async Task<dynamic> GetFriendsOnline()
        {
            var result = await services.friends.GetFriends(safeUserSession.userId);
            List<dynamic> onlineFriends = new List<dynamic>();
            foreach (FriendEntry friend in result)
            {
                if (!friend.isOnline)
                    continue;
                var onlineStatus = (await services.users.MultiGetPresence(new[] { friend.id })).First();
                onlineFriends.Add(new
                {
                    VisitorId = friend.id,
                    GameId = onlineStatus.gameId,
                    IsOnline = friend.isOnline,
                    LastOnline = onlineStatus.lastOnline,
                    LastLocation = onlineStatus.lastLocation,
                    LocationType = (int)onlineStatus.userPresenceType,
                    PlaceId = onlineStatus.placeId,
                    UserName = friend.name,
                });
            }
            return onlineFriends;
        }

        [HttpPost("users/filter-friends")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<dynamic> FilterFriends([FromForm] FilterSocialRequest request)
        {
            var result = await services.friends.GetFriends(request.userId);
            List<dynamic> filteredFriends = new List<dynamic>();
            foreach (FriendEntry friend in result)
            {
                if (!request.otherUserIds.Contains(friend.id))
                    continue;
                filteredFriends.Add(new
                {
                    Id = friend.id,
                    Username = friend.name,
                    AvatarUri = "http://",
                    AvatarFinal = true,
                    IsOnline = friend.isOnline,
                });
            }
            return filteredFriends;
        }

        [HttpPostBypass("friends/filter")]
        [HttpGetBypass("friends/filter")]
        public async Task<dynamic> GetFilteredFriends(string otherUserIds)
        {
            var ids = otherUserIds.Split(",").Select(long.Parse).Distinct().ToList();
            var result = await services.friends.GetFriends(safeUserSession.userId);
            List<dynamic> filteredFriends = new List<dynamic>();
            foreach (FriendEntry friend in result)
            {
                if (!ids.Contains(friend.id))
                    continue;
                filteredFriends.Add(new
                {
                    Id = friend.id,
                    Username = friend.name,
                    AvatarUri = "",
                    AvatarFinal = true,
                    IsOnline = friend.isOnline,
                });
            }
            return filteredFriends;
        }

        [HttpPost("user/multiget-friend-requests")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<dynamic> MultiGetFriendRequests([FromForm] FilterSocialRequest request)
        {
            var data = await services.friends.MultiGetFriendshipStatus(safeUserSession.userId, request.otherUserIds);

            List<dynamic> friendRequestsFromUser = new List<dynamic>();
            List<dynamic> friendRequestsToUser = new List<dynamic>();
            if (request.userId != safeUserSession.userId)
                throw new BadRequestException(7, "Invalid userId");
            foreach (var friend in data)
            {
                if (friend.status == "RequestReceived")
                {
                    friendRequestsFromUser.Add(new
                    {
                        SenderId = friend.id,
                        RecipientId = safeUserSession.userId
                    });
                }
                else if (friend.status == "RequestSent")
                {
                    friendRequestsToUser.Add(new
                    {
                        SenderId = safeUserSession.userId,
                        RecipientId = friend.id
                    });
                }
            }

            return new
            {
                FriendRequestsFromUser = friendRequestsFromUser,
                FriendRequestsToUser = friendRequestsToUser
            };
        }

        // [HttpPostBypass("friends/filter-requests")]
        // [HttpGetBypass("friends/filter-requests")]
        // public async Task<dynamic> GetFilteredFriendRequests(string otherUserIds)
        // {
        //     var ids = otherUserIds.Split(",").Select(long.Parse).Distinct().ToList();
        //     var data = await services.friends.MultiGetFriendshipStatus(safeUserSession.userId, ids);
        //     List<dynamic> friendRequestsFromUser = new List<dynamic>();
        //     List<dynamic> friendRequestsToUser = new List<dynamic>();
            
        //     foreach (var friend in data)
        //     {
        //         if (friend.status == "RequestReceived")
        //         {
        //             friendRequestsFromUser.Add(new
        //             {
        //                 SenderId = friend.id,
        //                 RecipientId = safeUserSession.userId
        //             });
        //         }
        //         else if (friend.status == "RequestSent")
        //         {
        //             friendRequestsToUser.Add(new
        //             {
        //                 SenderId = safeUserSession.userId,
        //                 RecipientId = friend.id
        //             });
        //         }
        //     }
        //     return new
        //     {
        //         FriendRequestsFromUser = friendRequestsFromUser,
        //         FriendRequestsToUser = friendRequestsToUser
        //     };
        // }

        [HttpPostBypass("user/unfriend")]
        public async Task<dynamic> UnfriendUserLegacy([FromForm] FriendRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            if (request.friendUserId == null)
                throw new BadRequestException(7, "FriendUserId is required");
            await services.friends.DeleteFriend(safeUserSession.userId, (long)request.friendUserId );
            return new
            {
                success = true,
                message = "Success"
            };
        }

        [HttpPostBypass("user/request-friendship")]
        public async Task<dynamic> RequestFriendshipLegacy([FromForm] FriendRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            if (safeUserSession.userId == request.recipientUserId)
                throw new BadRequestException(7, "The user cannot be friends with itself");
            if (request.recipientUserId == null)
                throw new BadRequestException(7, "RecipientUserId is required");
            await services.friends.RequestFriendship(safeUserSession.userId, (long)request.recipientUserId);
           
            return new
            {
                success = true,
                isCaptchaRequired = false,
            };
        }

        [HttpPostBypass("user/decline-friend-request")]
        public async Task<dynamic> DeclineFriendRequestLegacy([FromForm] FriendRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            if (request.requesterUserId == null)
                throw new BadRequestException(7, "RequesterUserId is required");
            await services.friends.DeclineFriendRequest(safeUserSession.userId, (long)request.requesterUserId);
            return new
            {
                success = true,
                message = "Success"
            };
        }
        [HttpPostBypass("user/accept-friend-request")]
        public async Task<dynamic> AcceptFriendRequestLegacy([FromForm] FriendRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            if (request.requesterUserId == null)
                throw new BadRequestException(7, "RequesterUserId is required");
            await services.friends.AcceptFriendRequest(safeUserSession.userId, (long)request.requesterUserId);
            return new
            {
                success = true,
                message = "Success"
            };
        }

        [HttpGetBypass("user/get-friendship-count")]
        public async Task<dynamic> GetFriendsAmount(long? userId)
        {
            return new
            {
                success = true,
                count = await services.friends.CountFriends(userId ?? safeUserSession.userId)
            };
        }
        // [HttpGetBypass("v1/users/{userId}/friends/statuses")]
        // public async Task<dynamic> MultiGetFriendshipStatus(string userIds)
        // {
        //     var ids = userIds.Split(",").Select(long.Parse).Distinct().ToList();

        //     if (ids.Count == 0 || ids.Count > 100)
        //         throw new BadRequestException();

        //     var data = await services.friends.MultiGetFriendshipStatus(safeUserSession.userId, ids);
        //     return new
        //     {
        //         data,
        //     };
        // }

        [HttpGetBypass("v1/users/{userId:long}/friends")]
        public async Task<RobloxCollection<FriendEntry>> GetUserFriends(long userId)
        {
            var result = await services.friends.GetFriends(userId);
            return new RobloxCollection<FriendEntry>()
            {
                data = result,
            };
        }

        [HttpGetBypass("v1/user/friend-requests/count")]
        public async Task<dynamic> GetFriendRequestCount()
        {
            return new
            {
                count =  await services.friends.GetFriendRequestCount(safeUserSession.userId),
            };
        }

        [HttpGetBypass("v1/users/{userId}/friends/count")]
        public async Task<dynamic> GetFriendCount(long userId)
        {
            return new
            {
                count = await services.friends.CountFriends((long)userId)
            };
        }
        [HttpGetBypass("v1/metadata")]
        public dynamic GetMetadata()
        {
            return new
            {
                isNearbyUpsellEnabled = false,
                isFriendsUserDataStoreCacheEnabled = false,
                userName = safeUserSession.username,
                displayName = safeUserSession.username,
            };
        }

        [HttpGetBypass("v1/my/friends/requests")]
        public async Task<dynamic> GetMyFriendRequests(string? cursor, int limit)
        {
            if (limit is <= 0 or > 100) limit = 10;
            return await services.friends.GetFriendRequests(safeUserSession.userId, cursor, limit);
        }

        [HttpPostBypass("v1/users/{userIdToRequest}/request-friendship")]
        public async Task<dynamic> RequestFriendship(long userIdToRequest)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            if (safeUserSession.userId == userIdToRequest)
                throw new BadRequestException(7, "The user cannot be friends with itself");
            await services.friends.RequestFriendship(safeUserSession.userId, userIdToRequest);

            return new
            {
                success = true,
                isCaptchaRequired = false,
            };
        }

        [HttpPostBypass("v1/users/{userIdToAccept:long}/accept-friend-request")]
        public async Task AcceptFriendRequest(long userIdToAccept)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            if (safeUserSession.userId == userIdToAccept)
                throw new BadRequestException(7, "The user cannot be friends with itself");

            await services.friends.AcceptFriendRequest(safeUserSession.userId, userIdToAccept);
        }

        [HttpPostBypass("v1/users/{userIdToDecline:long}/decline-friend-request")]
        public async Task DeclineFriendRequest(long userIdToDecline)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            await services.friends.DeclineFriendRequest(safeUserSession.userId, userIdToDecline);
        }

        [HttpPostBypass("v1/users/{userIdToRemove:long}/unfriend")]
        public async Task UnfriendUser(long userIdToRemove)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            await services.friends.DeleteFriend(safeUserSession.userId, userIdToRemove);
        }

        [HttpPostBypass("v1/users/{userIdToFollow:long}/follow")]
        public async Task<dynamic> FollowUser(long userIdToFollow)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FollowingEnabled);
            if (userIdToFollow == safeUserSession.userId)
                throw new BadRequestException();
            await services.friends.FollowerUser(safeUserSession.userId, userIdToFollow);

            return new
            {
                success = true,
                isCaptchaRequired = false,
            };
        }

        [HttpPostBypass("v1/users/{userIdToUnfollow:long}/unfollow")]
        public async Task DeleteFollowing(long userIdToUnfollow)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FollowingEnabled);
            await services.friends.DeleteFollowing(safeUserSession.userId, userIdToUnfollow);
        }

        [HttpGetBypass("v1/users/{userId:long}/followers/count")]
        public async Task<dynamic> CountFollowers(long userId)
        {
            var result = await services.friends.CountFollowers(userId);
            return new
            {
                count = result,
            };
        }

        [HttpGetBypass("v1/users/{userId:long}/followings/count")]
        public async Task<dynamic> CountFollowings(long userId)
        {
            var result = await services.friends.CountFollowings(userId);
            return new
            {
                count = result,
            };
        }

        [HttpGetBypass("v1/users/{userId:long}/followers")]
        public async Task<RobloxCollectionPaginated<FriendEntry>> GetFollowers(long userId, int limit, string? cursor)
        {
            if (limit is > 100 or < 1) limit = 10;
            return await services.friends.GetFollowers(userId, cursor, limit);
        }

        [HttpGetBypass("v1/users/{userId:long}/followings")]
        public async Task<RobloxCollectionPaginated<FriendEntry>> GetFollowings(long userId, int limit, string? cursor)
        {
            if (limit is > 100 or < 1) limit = 10;
            return await services.friends.GetFollowings(userId, cursor, limit);
        }

        [HttpPostBypass("v1/user/following-exists")]
        public async Task<dynamic> FollowingExists([Required,FromBody] FollowingExistsRequest request)
        {
            var result = new List<dynamic>();
            foreach (var userId in request.targetUserIds)
            {

                var isFollowing = await services.friends.IsOneFollowingTwo(safeUserSession.userId, userId);
                result.Add(new
                {
                    isFollowing,
                    userId,
                });
            }

            return new
            {
                followings = result,
            };
        }
    }
}