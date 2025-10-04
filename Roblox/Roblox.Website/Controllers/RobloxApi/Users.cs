using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Exceptions;
using Roblox.Dto.Users;
using Roblox.Models;
#pragma warning disable CS8600
namespace Roblox.Website.Controllers;

[Route("/")]
public class Users : ControllerBase
{
    [HttpPostBypass("v1/users")]
    public async Task<RobloxCollection<MultiGetEntry>> MultiGetUsersById([Required, FromBody] MultiGetRequest request)
    {
        var ids = request.userIds.ToList();
        if (ids.Count > 200 || ids.Count < 1)
        {
            throw new BadRequestException(0, "Invalid IDs");
        }

        var result = await services.users.MultiGetUsersById(ids);
        return new RobloxCollection<MultiGetEntry>()
        {
            data = result,
        };
    }
}