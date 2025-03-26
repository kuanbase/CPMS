using CPMS.Models;
using CPMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace CPMS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleController : ControllerBase
{
    private readonly IMongoCollection<Roles> _roles;
    private readonly IMongoCollection<Users> _users;

    public RoleController(MongoDBService mongoDBService)
    {
        _roles = mongoDBService.GetCollection<Roles>("Roles");
        _users = mongoDBService.GetCollection<Users>("Users");
    }

    [HttpGet("GetRoleList")]
    public IActionResult GetRoleList()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || (role != "User Manager" && role != "ADMINISTRATOR"))
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var roleList = _roles.Find(_ => true).ToList();

        return Ok(new { success = true, data = roleList });
    }

    [HttpGet("GetListByCurrentIndex")]
    public async Task<IActionResult> GetListByCurrentIndex([FromQuery] int page, [FromQuery] int pageSize)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = true, message = "Permission Denied!" });
        }

        var roleList = await _roles.Find(_ => true)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        if (roleList == null)
        {
            return BadRequest(new { success = false, message = "not found page" });
        }

        return Ok(new { success = true, data = roleList });
    }

    [HttpPost("CreateRole")]
    public async Task<IActionResult> CreateRole([FromBody] RequestBody requestBody)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = true, message = "Permission Denied!" });
        }

        var newRole = new Roles
        {
            Name = requestBody.name
        };

        try
        {
            await _roles.InsertOneAsync(newRole);
            return Ok(new { success = true, message = "Create role successful!" });
        }
        catch (Exception e)
        {
            return Problem($"Internal Server Error: {e.Message}", statusCode: 500);
        }
    }

    [HttpPut("ModifyRoleById")]
    public async Task<IActionResult> ModifyRoleById([FromBody] RequestBody requestBody)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var result = await _roles.UpdateOneAsync(r => r.Id == requestBody.Id, Builders<Roles>.Update.Set(r => r.Name, requestBody.name));

        if (result.ModifiedCount == 0 && result.MatchedCount > 0)
        {
            return BadRequest(new { success = false, message = "Everything updated already!" });
        }
        else if (result.ModifiedCount <= 0)
        {
            return BadRequest(new { success = false, message = "Role doesn't exist!" });
        }

        return Ok(new { success = true, message = "Role data update successful!" });
    }

    [HttpPost("RemoveById")]
    public async Task<IActionResult> RemoveById([FromBody] RequestBody requestBody)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var exists = await _users.Find(u => u.RoleId == requestBody.Id).AnyAsync();

        if (exists)
        {
            return BadRequest(new { success = false, message = "not empty!" });
        }

        var result = await _roles.DeleteOneAsync(r => r.Id == requestBody.Id);

        if (result.DeletedCount <= 0)
        {
            return BadRequest(new { success = false, message = "Not found!" });
        }

        return Ok(new { success = true, message = "Delete successful!" });
    }

    public class RequestBody
    {
        public string? Id { get; set; }
        public string? name { get; set; }
    }
}
