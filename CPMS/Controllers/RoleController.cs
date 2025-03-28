using CPMS.Models;
using CPMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Serilog;
using System.Security.Claims;

namespace CPMS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleController : ControllerBase
{
    private readonly IMongoCollection<Roles> _roles;
    private readonly IMongoCollection<Users> _users;
    private readonly string _ip;

    public RoleController(MongoDBService mongoDBService, IHttpContextAccessor httpContextAccessor)
    {
        _roles = mongoDBService.GetCollection<Roles>("Roles");
        _users = mongoDBService.GetCollection<Users>("Users");
        _ip = httpContextAccessor.HttpContext!.Connection.RemoteIpAddress!.ToString();
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
    public async Task<IActionResult> CreateRole([FromBody] RoleRequestBody roleRequestBody)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = true, message = "Permission Denied!" });
        }

        var newRole = new Roles
        {
            Name = roleRequestBody.name
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
    public async Task<IActionResult> ModifyRoleById([FromBody] RoleRequestBody roleRequestBody)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var result = await _roles.UpdateOneAsync(r => r.Id == roleRequestBody.Id, Builders<Roles>.Update.Set(r => r.Name, roleRequestBody.name));

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
    public async Task<IActionResult> RemoveById([FromBody] RoleRequestBody roleRequestBody)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var exists = await _users.Find(u => u.RoleId == roleRequestBody.Id).AnyAsync();

        if (exists)
        {
            return BadRequest(new { success = false, message = "not empty!" });
        }

        var result = await _roles.DeleteOneAsync(r => r.Id == roleRequestBody.Id);

        if (result.DeletedCount <= 0)
        {
            return BadRequest(new { success = false, message = "Not found!" });
        }

        return Ok(new { success = true, message = "Delete successful!" });
    }

    [HttpGet("GetRoleAmount")]
    public async Task<IActionResult> GetRoleAmount()
    {
        var name = User.FindFirst(ClaimTypes.Name)?.Value;

        if (name == null)
        {
            Log.Fatal($"有可疑用戶，沒有名字。 IP Address: {_ip}");
            return BadRequest(new { success = false, message = "Why your name is null???" });
        }

        try
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == null || (role != "User Manager" && role != "ADMINISTRATOR"))
            {
                return Unauthorized(new { success = false, message = "Permission Denied!" });
            }

            var amount = await _roles.Find(_ => true).CountDocumentsAsync();

            Log.Information($"獲取用戶數量成功, 操作人: {name}, IP Address: {_ip}");

            return Ok(new { success = true, data = amount });
        }
        catch (Exception ex)
        {
            Log.Fatal($"獲取用戶數量時，發生意外: {ex}");
            return StatusCode(500, $"獲取用戶數量時，發生意外: {ex}, 操作人: {name}, IP Address: {_ip}");
        }
    }

    public class RoleRequestBody
    {
        public string? Id { get; set; }
        public string? name { get; set; }
    }
}
