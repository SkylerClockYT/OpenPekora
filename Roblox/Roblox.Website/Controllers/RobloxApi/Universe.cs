using System.Collections;
using System.ComponentModel;
using System.Numerics;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roblox.Dto.Assets;
using Roblox.Dto.Games;
using Roblox.Dto.Users;
using Roblox.Exceptions;
using Roblox.Libraries.Cursor;
using Roblox.Logging;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Models.Db;
using Roblox.Models.Studio;
using Roblox.Services.App.FeatureFlags;
using Roblox.Services.Exceptions;
using Roblox.Website.Pages.Auth;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/")]
public class UniverseV1 : ControllerBase
{
    [HttpGetBypass("toolbox-service/v1/{type}")]
    public async Task<dynamic> GetToolBoxService([FromRoute] string type, [FromQuery] string sortType, [FromQuery] int limit = 30, [FromQuery] string? cursor = null, [FromQuery] string? keyword = null)
    {
        CatalogSearchRequest request = new CatalogSearchRequest
        {
            keyword = keyword,
            category = type,
            subcategory = type,
            sortType = sortType,
            limit = limit,
            cursor = cursor
        };
        var searchResults = await services.assets.SearchCatalog(request);
        return new
        {
            totalResults = searchResults.data!.Count(),
            filteredKeyword = searchResults.keyword,
            searchDebugInfo = (string?)null,
            spellCheckerResult = new
            {
                correctionState = 0,
                correctedQuery = (string?)null,
                userQuery = (string?)null,
            },
            queryFacets = new
            {
                appliedFacets = new List<object>(),
                availableFacets = new List<object>(),
            },
            imageSearchStatus = (string?)null,
            previousPageCursor = searchResults.previousPageCursor,
            nextPageCursor = searchResults.nextPageCursor,
            data = searchResults.data!.Select(c => new
            {
                id = c.id,
                name = (string?)null,
                searchResultSource = "LexicalWithSort"
            })
        };
    }
    [HttpPostBypass("toolbox-service/v1/items/details")]
    public async Task<dynamic> GetToolBoxServiceDetails([FromBody] WebsiteModels.Catalog.MultiGetRequest request)
    {
        var multiGetResults = await services.assets.MultiGetInfoById(request.items.Select(c => c.id));
        return new
        {
            data = multiGetResults.Select(c =>
            {
                return new
                {
                    asset = new
                    {
                        audioDetails = (string?)null,
                        id = c.id,
                        name = c.name,
                        typeId = (int)c.assetType,
                        assetSubTypes = new List<int>(),
                        assetGenres = c.genres,
                        isEndorsed = false,
                        description = c.description,
                        duration = 0,
                        hasScripts = c.assetType == Models.Assets.Type.Model || c.assetType == Models.Assets.Type.Plugin,
                        createdUtc = c.createdAt,
                        updatedUtc = c.updatedAt,
                        creatingUniverseId = (string?)null,
                        isAssetHashApproved = c.moderationStatus == ModerationStatus.ReviewApproved,
                        // TODO: Asset privacy options
                        visibilityStatus = c.moderationStatus == ModerationStatus.ReviewApproved,
                        socialLinks = new List<object>(),
                    },
                    creator = new
                    {
                        id = c.creatorTargetId,
                        name = c.creatorName,
                        type = (int)c.creatorType,
                        isVerifiedCreator = false,
                        latestGroupUpdaterUserId = (string?)null,
                        latestGroupUpdaterUserName = (string?)null,
                    },
                    // TODO: Votes
                    voting = new
                    {
                        showVotes = false,
                        upVotes = 0,
                        downVotes = 0,
                        canVote = false,
                        userVote = (string?)null,
                        hasVoted = false,
                        voteCount = 0,
                        upVotePercent = 0,
                    },
                    fiatProduct = new
                    {
                        currencyCode = "USD",
                        quantity = new
                        {
                            significand = 0,
                            exponent = 0,
                        },
                        published = true,
                        purchasable = true,
                    }
                };
            })
        };
    }
    [HttpGetBypass("universes/get-universe-containing-place")]
    public async Task<dynamic> GetUniverse(long placeid)
    {
        return new
        {
            UniverseId = await services.games.GetUniverseId(placeid)
        };
    }

    [HttpGetBypass("universes/get-info")]
    public async Task<dynamic> GetUniverseInfo(long universeId)
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        if (uni == null)
            throw new RecordNotFoundException();
        return new
        {
            Name = uni.name,
            Description = uni.description,
            RootPlace = uni.rootPlaceId,
            StudioAccessToApisAllowed = true,
            CurrentUserHasEditPermissions = uni.creatorId == safeUserSession.userId,
            UniverseAvatarType = uni.universeAvatarType,
        };
    }

    [HttpGetBypass("universes/get-universe-places")]
    public async Task<dynamic> GetUniversePlaces(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var rootPlace = await services.games.GetRootPlaceId(universeId);
        var places = await services.games.GetUniversePlaces(universeId);
        return new
        {
            FinalPage = true,
            RootPlace = rootPlace,
            Places = places.Select(placeInfo => new
            {
                PlaceId = placeInfo.placeId,
                Name = placeInfo.name,
            }),
            PageSize = places.Count()
        };
    }

    [HttpGetBypass("badges/list-badges-for-place/json")]
    public async Task<dynamic> GetGameBadges(long placeId)
    {
        var universeId = await services.games.GetUniverseId(placeId);
        var universe = await services.games.GetUniverseInfo(universeId);
        var badges = (await services.badges.GetBadgesForUniverse(universe, 100, 0, Models.Db.SortOrder.Desc)).ToList();
        return new
        {
            FinalPage = true,
            PlaceId = placeId,
            GameBadges = badges.Select((BadgeAssetDetails c) => new
            {
                BadgeAssetId = c.id,
                CreatorId = universe.creatorId,
                ImageUrl = Configuration.BaseUrl + "/Thumbs/Asset.ashx?assetId=" + c.id,
                IsImageUrlFinal = true,
                Name = c.displayName,
                Description = c.description,
                IsOwned = false,
                Rarity = c.statistics.winRatePercentage,
                TotalAwarded = c.statistics.awardedCount,
                TotalAwardedYesterday = c.statistics.pastDayAwardedCount,
                Created = c.created,
                Updated = c.updated,
                RarityName = services.badges.GetDifficultyFromPercentage(c.statistics.winRatePercentage).ToString(),
            }),
            PageSize = badges.Count
        };
    }

    [HttpGetBypass("developerproducts/list")]
    public async Task<dynamic> GetDeveloperProducts(long page, long? placeId, long? universeId)
    {
        if (page < 1 || page > 5)
        {
            page = 1;
        }

        if (universeId is null && placeId is not null)
        {
            universeId = await services.games.GetUniverseId(placeId.Value);
        }
        else if (universeId == null)
        {
            throw new BadRequestException(0, "You must provide a valid placeId or universeId.");
        }

        var products = (await services.games.GetDeveloperProducts(universeId.Value, 5, 5 * (page - 1))).ToList();
        return new
        {
            FinalPage = products.Count < 5 || page == 5,
            DeveloperProducts = products.Select(c => new
            {
                ProductId = c.id,
                DeveloperProductId = c.iconImageAssetId,
                Name = c.name,
                Description = c.description,
                IconImageAssetId = c.iconImageAssetId,
                displayName = c.name,
                displayDescription = c.description,
                displayIcon = (int?)null,
                PriceInRobux = c.priceInRobux,
            }),
            PageSize = products.Count
        };
    }

    [HttpGetBypass("universes/get-aliases")]
    public dynamic GetAliases()
    {
        return new
        {
            FinalPage = true,
            Aliases = new List<string>(),
            PageSize = 50
        };
    }

    [HttpGet("v1/gametemplates")]
    public async Task<dynamic> StudioTemplates()
    {
        // ArrayList templates = new ArrayList();
        // int i = 1;
        // foreach (var place in GetStarterPlaces) {
        //     templates.Add(new {
        //         gameTemplateType = "Generic",
        //         hasTutorials = false,
        //         universe = new Universe {
        //             id = i,
        //             name = place.Key,
        //             description = "skibidi",
        //             isArchived = false,
        //             rootPlaceId = place.Value,
        //             isActive = true,
        //             privacyType = "Public",
        //             creatorType = "User",
        //             creatorTargetId = 1,
        //             creatorName = "ROBLOX",
        //             created = DateTime.Parse("2013-11-01T08:47:14.07Z"),
        //             updated = DateTime.Parse("2023-05-02T22:03:01.107Z")
        //         }
        //     });
        //     i++;
        // }

        var templates = await services.games.MultiGetPlaceDetails(services.assets.getStarterPlaces.Values.ToList()); //await services.games.MultiGetUniverseInfo(getStarterPlaces.Values.ToList());
        return new
        {
            data = templates.Select(c => new
            {
                gameTemplateType = "Generic",
                hasTutorials = false,
                universe = new 
                {
                    id = c.universeId,
                    name = c.name,
                    description = c.description ?? "skbidii",
                    isArchived = false,
                    rootPlaceId = c.universeRootPlaceId,
                    isActive = true,
                    privacyType = "Public",
                    creatorType = "User",
                    creatorTargetId = c.builderId,
                    creatorName = c.builder,
                    created = c.created,
                    updated = c.updated
                }
            })
        };
    }

    [HttpGetBypass("v1/universes/multiget")]
    public async Task<dynamic> MultiGetUniverseInfo([FromQuery] List<long> ids)
    {
        var universes = await services.games.MultiGetUniverseInfo(ids);
        return new
        {
            data = universes.Select(c =>
            {
                return new
                {
                    id = c.id,
                    name = c.name,
                    description = c.description,
                    isArchived = false,
                    rootPlaceId = c.rootPlaceId,
                    isActive = c.privacyType != PrivacyType.Private,
                    privacyType = c.isPublic ? "Public" : "Private",
                    creatorType = c.creatorType,
                    creatorTargetId = c.creatorId,
                    creatorName = c.creatorName,
                    created = c.created,
                    updated = c.updated
                };
            })
        };
    }

    [HttpGet("v1/search/universes")]
    public async Task<dynamic> SearchUniverse(string q, int limit = 10, SortOrder sortOrder = SortOrder.Asc, string? cursor = null)
    {
        if (limit is > 100 or < 1) limit = 10;
        int offset = int.Parse(cursor ?? "0");
        if (q.Contains("Team"))
        {
            var result = await services.games.GetEditableUniversesForUser(safeUserSession.userId);
            return new
            {
                previousPageCursor = (string?)null,
                nextPageCursor = (string?)null,
                data = result.Select(c =>
                {
                    return new
                    {
                        id = c.id,
                        name = c.name,
                        description = c.description,
                        isArchived = false,
                        rootPlaceId = c.rootPlaceId,
                        isActive = c.privacyType != PrivacyType.Private,
                        privacyType = c.isPublic ? PrivacyType.Public : PrivacyType.Private,
                        creatorType = c.creator.type,
                        creatorTargetId = c.creatorId,
                        creatorName = c.creatorName,
                        created = c.created,
                        updated = c.updated
                    };
                })
            };
        }
        else
        {
            var result =
                (await services.games.GetGamesForTypeDevelop(CreatorType.User, safeUserSession.userId,
                    safeUserSession.username, limit, offset, sortOrder.ToString(), null)).ToList();
            return new RobloxCollectionPaginated<GamesForCreatorDevelop>()
            {
                previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
                nextPageCursor = result.Count >= limit ? (offset + limit).ToString() : null,
                data = result
            };
        }
    }

    [HttpGet("v1/user/universes")]
    public async Task<RobloxCollectionPaginated<GamesForCreatorDevelop>> GetUserCreatedGames(string? sortOrder, string? accessFilter, int limit, string? cursor = null)
    {
        if (limit is > 100 or < 1) limit = 10;
        int offset = int.Parse(cursor ?? "0");
        var result =
            (await services.games.GetGamesForTypeDevelop(CreatorType.User, safeUserSession.userId,
                safeUserSession.username, limit, offset, sortOrder ?? "asc", accessFilter ?? "All")).ToList();
        return new RobloxCollectionPaginated<GamesForCreatorDevelop>()
        {
            nextPageCursor = result.Count >= limit ? (offset + limit).ToString() : null,
            previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
            data = result
        };
    }


    [HttpGetBypass("v1/user/teamcreate/memberships")]
    public async Task<dynamic> GetMembershipsForCurrentUser()
    {
        var memberships = await services.games.GetEditableUniversesForUser(safeUserSession.userId);
        return new
        {
            previousPageCursor = (string?)null,
            nextPageCursor = (string?)null,
            data = memberships.Select(c =>
            {
                return new
                {
                    id = c.id,
                    name = c.name,
                    description = c.description,
                    isArchived = false,
                    rootPlaceId = c.rootPlaceId,
                    isActive = c.privacyType != PrivacyType.Private,
                    privacyType = c.isPublic ? PrivacyType.Public : PrivacyType.Private,
                    creatorType = c.creator.type,
                    creatorTargetId = c.creatorId,
                    creatorName = c.creatorName,
                    created = c.created,
                    updated = c.updated
                };
            })
        };
    }

    [HttpGetBypass("v1/universes/{universeId}/teamcreate/memberships")]
    public async Task<dynamic> GetMembershipsForUniverse(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var memberships = await services.games.GetUniversePermissions(universeId);
        var userInfo = await services.users.MultiGetUsersById(memberships.Select(c => c.subjectId).Distinct());

        return new
        {
            previousPageCursor = (string?)null,
            nextPageCursor = (string?)null,
            data = memberships.Where(c => c.action != 0).Select(c =>
            {
                var user = userInfo.FirstOrDefault(u => u.id == c.subjectId);
                return new
                {
                    buildersClubMembershipType = "None",
                    userId = user!.id,
                    username = user.displayName,
                    displayName = user.displayName,
                };
            })
        };
    }

    /*
    [HttpGetBypass("teamtest/{placeId}/runninggames")]
    [HttpGet("v1/teamtest/places/{placeId}/runninggames")]
    public dynamic GetTeamTestRunningGames(long placeId)
    {
        return new
        {
            FinalPage = true,
            RunningGames = new List<dynamic>(),
            PageSize = 50
        };
    }
    */
    [HttpGetBypass("universes/{universeId}/listcloudeditors")]
    public async Task<dynamic> GetCloudEditors(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var editors = await services.games.GetUniversePermissions(universeId);
        var userInfo = await services.users.MultiGetUsersById(editors.Select(c => c.subjectId).Distinct());
        return new
        {
            finalPage = true,
            users = editors.Where(c => c.action != 0).Select(c =>
            {
                var user = userInfo.FirstOrDefault(u => u.id == c.subjectId);
                return new
                {
                    userId = user!.id,
                    isAdmin = false,
                };
            })
        };
    }

    [HttpGet("v1/places/{placeId}/teamcreate/active_session/members")]
    public async Task<dynamic> GetTeamCreateMembers(long placeId)
    {
        List<dynamic> players = new List<dynamic>();
        var startIndex = 0;
        var limit = 1;
        var offset = startIndex;
        var servers = (await services.gameServer.GetGameServers(placeId, offset, limit, 3)).ToList();

        foreach (var server in servers)
        {
            var gameServerPlayers = server.players.Select(player => new
            {
                id = player.userId,
                name = player.username,
                displayName = player.username
            }).ToList();

            players.AddRange(gameServerPlayers);
        }

        return new
        {
            data = players
        };
    }

    [HttpGet("v1/user/groups/canmanage")]
    public dynamic CanManageGroup()
    {
        return new
        {
            data = new List<object>()
        };
    }

    [HttpGet("v2/universes/{universeId}/permissions")]
    public async Task<dynamic> GetUniversePermissions(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var permissions = await services.games.GetUniversePermissions(universeId);
        var userInfo = await services.users.MultiGetUsersById(permissions.Select(c => c.subjectId).Distinct());
        return new
        {
            data = permissions.Select(c =>
            {
                var user = userInfo.FirstOrDefault(u => u.id == c.subjectId);
                return new
                {
                    userId = user!.id,
                    userName = user.displayName,
                    action = c.action,
                    allowedPermissions = "Play,Edit"
                };
            })
        };
    }

    [HttpDeleteBypass("/v2/universes/{universeId}/permissions_batched")]
    public async Task<dynamic> DeleteUniversePermissionsBatched(long universeId) 
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var request = JsonConvert.DeserializeObject<List<UniversePermission>>(await GetRequestBody())!;

        await services.games.BatchDeleteUniversePermissions(request, universeId);

        return Content("{}", "application/json");
    }

    [HttpPostBypass("/v2/universes/{universeId}/permissions_batched")]
    public async Task<dynamic> SetUniversePermissionsBatched(long universeId) 
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var request = JsonConvert.DeserializeObject<List<UniversePermission>>(await GetRequestBody())!;

        await services.games.BatchUpdateUniversePermissions(request, universeId);

        return Content("{}", "application/json");
    }
    [HttpGetBypass("v1/universes/{universeId}/context-permission")]
    [HttpGetBypass("v1/universes/{universeId}/permissions")]
    public async Task<dynamic> CanManage(long universeId) 
    {
        bool canManage = true;
        try 
        {
            await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        }
        catch (Exception)
        {
            canManage = false;
        }

        bool canCloudEdit = await services.games.CanEditUniverse(safeUserSession.userId, universeId) || canManage;
        return new 
        {
            canManage,
            canCloudEdit
        };
    }

    [HttpPostBypass("/v1/universes/{universeId}/teamcreate")]
    [HttpPatchBypass("/v1/universes/{universeId}/teamcreate")]
    public async Task<dynamic> SetTeamCreateSettings([FromRoute] long universeId) 
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        // [FromBody] doesnt work
        var request = JsonConvert.DeserializeObject<TeamCreateSettings>(await GetRequestBody())!;
        await services.games.SetCloudedit(request.isEnabled, universeId);

        return Content("{}", "application/json");
    }

    [HttpGetBypass("v1/universes/{universeId}/teamcreate")]
    public async Task<dynamic> TeamCreateSettings(long universeId) 
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        return new
        {
            isEnabled = await services.games.IsCloudeditEnabled(universeId),
        };
    }

    [HttpGetBypass("v1/universes/{universeId}")]
    public async Task<dynamic> UniverseInfo(long universeId) 
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        if (uni == null)
            throw new RecordNotFoundException();
        var assetInfo = (await services.assets.MultiGetAssetDeveloperDetails(new[] { uni.rootPlaceId })).First();
        return new 
        {
            id = universeId,
            name = uni.name,
            description = uni.description,
            isArchived = false,
            rootPlaceId = uni.rootPlaceId,
            isActive = uni.privacyType != PrivacyType.Private,
            privacyType = uni.isPublic ? PrivacyType.Public : PrivacyType.Private,
            creatorType = assetInfo.creator.type,
            creatorTargetId = uni.creatorId,
            creatorName = uni.creatorName,
            created = uni.created,
            updated = uni.updated
        };
    }

    [HttpGetBypass("v1/universes/{universeId}/icon")]
    public dynamic GetUniverseIcon(long universeId) 
    {
        return new 
        {
            imageId = (int?)null,
            isApproved = true
        };
    }

    [HttpPatchBypass("v1/universes/{universeId}/configuration")]
    [HttpPatchBypass("v2/universes/{universeId}/configuration")]
    public async Task<dynamic> SetUniverseConfiguration([FromRoute] long universeId, [FromBody] UpdateUniverseConfiguration configuration) 
    {
        List<string> playableDevices = new List<string> {
            "Computer",
            "Phone",
            "Tablet",
            "Console",
            "VR"
        };
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        

        //await services.games.SetPlaceVisibility(universeId, configuration.privacyType == PrivacyType.Public);

        if (configuration.universeAvatarType != null)
            await services.games.SetForceMorph(universeId, configuration.universeAvatarType == "PlayerChoice" ? ForceMorphType.PlayerChoice : configuration.universeAvatarType == "MorphToR6" ? ForceMorphType.MorphToR6 : ForceMorphType.MorphToR15);
        if (configuration.isFriendsOnly != null)
        {
            if ((bool)configuration.isFriendsOnly)
            {
                await services.games.SetPlacePrivacyType(universeId, PrivacyType.FriendsOnly);
            }
            else
            {
                await services.games.SetPlacePrivacyType(universeId, PrivacyType.Public);
            }
        }

        var uni = await services.games.SafeGetUniverseInfo(safeUserSession.userId, universeId);

        return new
        {
            allowPrivateServers = false,
            privateServerPrice = 0,
            id = universeId,
            name = uni.name,
            universeAvatarType = uni.universeAvatarType,
            universeScaleType = "AllScales",
            universeAnimationType = "PlayerChoice",
            universeCollisionType = R15CollisionType.OuterBox.ToString(),
            universeBodyType = "Standard",
            universeJointPositioningType = "ArtistIntent",
            universeAvatarMinScales = new
            {
                height = 0.90,
                width = 0.70,
                head = 0.90,
                depth = 0.70,
                proportion = 0,
                bodyType = 0,
            },
            universeAvatarMaxScales = new
            {
                height = 1.05,
                width = 1,
                head = 1,
                depth = 1,
                proportion = 1,
                bodyType = 1,
            },
            isArchived = false,
            isFriendsOnly = uni.privacyType == PrivacyType.FriendsOnly,
            genre = uni.genre,
            playableDevices = playableDevices,
            permissions = new
            {
                IsThirdPartyTeleportAllowed = true,
                IsThirdPartyAssetAllowed = true,
                IsThirdPartyPurchaseAllowed = true,
            },
            isForSale = false,
            price = 0,
            studioAccessToApisAllowed = true,
            isStudioAccessToApisAllowed = true,
            privacyType = uni.isPublic ? PrivacyType.Public : PrivacyType.Private,
        };
    }

    [HttpGet("v2/universes/{universeId}/configuration")]
    [HttpGet("v1/universes/{universeId}/configuration")]
    public async Task<dynamic> GetUniverseConfiguration(long universeId) 
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        List<string> playableDevices = new List<string> 
        {
            "Computer",
            "Phone",
            "Tablet",
            "Console",
            "VR"
        };
        if (uni == null)
            throw new RecordNotFoundException();
        return new  
        {
            allowPrivateServers = false,
            privateServerPrice = 0,
            id = universeId,
            name = uni.name,
            universeAvatarType = uni.universeAvatarType,
            universeScaleType = "AllScales",
            universeAnimationType = "PlayerChoice",
            universeCollisionType = R15CollisionType.OuterBox.ToString(),
            universeBodyType = "Standard",
            universeJointPositioningType = "ArtistIntent",
            universeAvatarMinScales = new
            {
                height = 0.90,
                width = 0.70,
                head = 0.90,
                depth = 0.70,
                proportion = 0,
                bodyType = 0,
            },
            universeAvatarMaxScales = new
            {
                height = 1.05,
                width = 1,
                head = 1,
                depth = 1,
                proportion = 1,
                bodyType = 1,
            },
            isArchived = false,
            isFriendsOnly = uni.privacyType == PrivacyType.FriendsOnly,
            genre = uni.genre,
            playableDevices = playableDevices,
            permissions = new 
            {
                IsThirdPartyTeleportAllowed = true,
                IsThirdPartyAssetAllowed = true,
                IsThirdPartyPurchaseAllowed = true,
            },
            isForSale = false,
            price = 0,
            studioAccessToApisAllowed = true,
            isStudioAccessToApisAllowed = true,
            privacyType = uni.isPublic ? PrivacyType.Public : PrivacyType.Private,
        };
    }

    [HttpPostBypass("v1/universes/{universeId}/activate")]
    public async Task<dynamic> ActivateUniverse(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        await services.games.SetPlacePrivacyType(universeId, PrivacyType.Public);
        return new { };
    }

    [HttpPostBypass("v1/universes/{universeId}/deactivate")]
    public async Task<dynamic> DeactivateUniverse(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        await services.games.SetPlacePrivacyType(universeId, PrivacyType.Private);
        return new { };
    }

    [HttpGetBypass("/universal-app-configuration/v1/behaviors/studio/content")]
    public dynamic GetStudioContent()
    {
        return new {};
    }

    // public dynamic GetBoilerplateStudioContent() {
    //     return new {
    //         FinalPage = true,
    //         DeveloperProducts = Array.Empty<string>(),
    //         PageSize = 5
    //     };
    // }
        
    [HttpGetBypass("/v1/universes/{universeId:long}/symbolic-links")]
    public dynamic GetBoilerplateContent()
    {
        return new
        {
            previousPageCursor = (string?)null,
            nextPageCursor = (string?)null,
            data =  Array.Empty<string>()
        };
    }
    [HttpGetBypass("/v1/game-localization-roles/games/{universeId:long}/current-user/roles")]
    public dynamic GetCurrentUserRoles()
    {
        
        return new
        {
            data = Array.Empty<string>()
        };
    }

    [HttpPostBypass("/v1/autolocalization/games/{universeId:long}/autolocalizationtable")]
    public dynamic GetAutoLocalizationTable()
    {
        return new
        {
            supportedLocales = new List<dynamic>
            {
                new
                {
                    id = 1,
                    locale = "en_us",
                    name = "English(US)",
                    nativeName = "English",
                    language = new
                    {
                        id = 41,
                        name = "English",
                        nativeName = "English",
                        languageCode = "en",
                        isRightToLeft = false
                    }
                }
            }
        };
    }
    
    [HttpGetBypass("/universes/create")]
    [HttpPostBypass("/universes/create")]
    public async Task<dynamic> CreateUniverseApi([FromBody] CreateUniverseRequest request)
    {
        if (!FeatureFlags.IsEnabled(FeatureFlag.CreatePlaceSelfService))
            throw new BadRequestException(0, "Place creation is currently disabled");
        if (userSession is null)
            throw new UnauthorizedException(0, "You are not logged in");
        await using var createGameLock =
            await Roblox.Services.Cache.redLock.CreateLockAsync("CreatePlaceSelfServiceV1:UserId:" + userSession.userId,
                TimeSpan.FromSeconds(10));
        if (!createGameLock.IsAcquired)
        {
            Writer.Info(LogGroup.AbuseDetection, "CreatePlace API could not acquire createGameLock");
            throw new TooManyRequestsException(0, "Too many attempts. Try again in a few seconds.");
        }
        
        var createStatus = await CanCreatePlace(userSession.userId);
        if (createStatus != PlaceCreationFailureReason.Ok) 
            throw new BadRequestException(0, GetMessage(createStatus));
        

        Writer.Info(LogGroup.AbuseDetection, "CreatePlace API userId={0} can create a place, creating it", userSession.userId);
        // create one!
        var asset = await services.assets.CreatePlace(userSession.userId, userSession.username, CreatorType.User, userSession.userId, request.templatePlaceIdToUse);
        // create universe too
        var universe = await services.games.CreateUniverse(asset.placeId);
        // give url
        return new
        {
            asset.placeId,
            universe.universeId,
        };
    }
    
    public async Task<PlaceCreationFailureReason> CanCreatePlace(long userId)
    {
        var log = Writer.CreateWithId(LogGroup.AbuseDetection);
        log.Info("start CanCreatePlace with userId={0}",userId);
        var userInfo = await services.users.GetUserById(userId);
        if (userInfo.created > DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)))
        {
            log.Info("account is too new");
            return PlaceCreationFailureReason.AccountTooNew;
        }

        var createdPlaces = (await services.assets.GetCreations(CreatorType.User, userId, Type.Place, 0, 100)).ToArray();
        if (createdPlaces.Length != 0)
        {
            if (createdPlaces.Length > 15)
            {
                log.Info("account has too many places {0}", createdPlaces.Length);
                return PlaceCreationFailureReason.TooManyPlaces;
            }

            var placeDetails = (await services.games.MultiGetPlaceDetails(createdPlaces
                    .Select(c => c.assetId)))
                .ToArray();

            if (placeDetails.Length != createdPlaces.Length)
            {
                // uhhh
                log.Info("placeDetails len and createdPlaces len do not match: {0} vs {1}", placeDetails.Length, createdPlaces.Length);
                if (placeDetails.Length == 0)
                    throw new Exception("Place details len is zero while createdPlaces len is not zero");
            }


            var isAnyPlaceCreatedLessThanADayAgo =
                placeDetails.FirstOrDefault(v => v.created > DateTime.UtcNow.Subtract(TimeSpan.FromDays(1))) != null;

            if (isAnyPlaceCreatedLessThanADayAgo && !(userId is 3 or 1 or 7))
            {
                log.Info("account place was created less than a day ago");
                return PlaceCreationFailureReason.LatestPlaceCreatedTooRecently;
            }
        }


        var app = await services.users.GetApplicationByUserId(userId);
        if (app is not {status: UserApplicationStatus.Approved})
        {
            log.Info("user has no app or it is not approved {0}", app?.status.ToString());
            return PlaceCreationFailureReason.NoApplication;
        }
        
        log.Info("user is active enough for a place. return OK");
        return PlaceCreationFailureReason.Ok;
    }
    
    private string GetMessage(PlaceCreationFailureReason reason)
    {
        return reason switch
        {
            PlaceCreationFailureReason.AccountTooNew =>
                "Your account is too new. Try again when your account is at least 7 days old.",
            PlaceCreationFailureReason.TooManyPlaces => "Your account already has the maximum amount of places on it.",
            PlaceCreationFailureReason.NoApplication => "You cannot create a place if you did not join through the application system.",
            PlaceCreationFailureReason.TooInactive => "Your account is too inactive to create a place. Staff cannot comment on the exact reason, so please do not ask. Try playing around some more, posting on places like the forums, joining groups, buying items, then try again in a few days.",
            PlaceCreationFailureReason.LatestPlaceCreatedTooRecently => "Latest place was created too recently. Try again in a day.",
            PlaceCreationFailureReason.NotEnoughVisitsForNewPlace => "You do not have enough visits to create a new place. Try again in a few days.",
            _ => "Unknown reason. Code = " + reason.ToString(),
        };
    }
    
    private string GetRedisKeyForRejection(long userId)
    {
        return "app_rejected_recently_for_place:v1.2:" + userId;
    }

    private async Task<bool> WasRejectedRecently(long userId)
    {
        var result = await Roblox.Services.Cache.distributed.StringGetAsync(GetRedisKeyForRejection(userId));
        if (result != null)
            return true;
        return false;
    }
    
    public enum PlaceCreationFailureReason
    {
        Ok = 1,
        AccountTooNew,
        TooManyPlaces,
        NoApplication,
        TooInactive,
        LatestPlaceCreatedTooRecently,
        NotEnoughVisitsForNewPlace,
    }
}