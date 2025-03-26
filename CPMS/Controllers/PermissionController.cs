using CPMS.Models;
using CPMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace CPMS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PermissionController : ControllerBase
{
    private readonly IMongoCollection<Permissions> _premissions;
    private readonly IMongoCollection<Roles> _roles;
    private readonly IMongoCollection<Users> _users;

    public PermissionController(MongoDBService mongoDBService)
    {
        _premissions = mongoDBService.GetCollection<Permissions>("Premissions");
        _roles = mongoDBService.GetCollection<Roles>("Roles");
        _users = mongoDBService.GetCollection<Users>("Users");
    }

    [HttpGet("GetPermissionList")]
    public IActionResult GetPermissionList()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var permissionList = _premissions.Find(_ => true).ToList();

        return Ok(new { success = true, data = permissionList });
    }

    [HttpPost("NewRole")]
    public IActionResult NewPermission([FromBody] Permissions permissionsModel)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        _premissions.InsertOne(permissionsModel);

        return Ok(new { success = true, message = "Create a new permission successful!" });
    }

    [HttpPut("ModifyRole")]
    public IActionResult ModifyRole([FromBody] Permissions permissionsModel)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var permission = _premissions.Find(p => p.Name == permissionsModel.Name).FirstOrDefault();
        
        if (permission == null)
        {
            return BadRequest();
        }

        return Ok(new { success = true, message = "Modify a permission successful!" });
    }

    [HttpPost("DeleteRole")]
    public IActionResult DeleteRole([FromBody] Permissions permissionsModel)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "ADMINISTRATOR")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        return Ok(new { success = true, message = "Delete the permission successful!" });
    }

    [HttpGet("GetPermissionByOnwer")]
    public IActionResult GetPermissionByOwner()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null)
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var roles = _roles.Find(_ => true).FirstOrDefault();

        if (roles == null)
        {
            return Problem("Internal Server Erorr", statusCode: 500);
        }

        var permission = User.FindAll("Permissions")?.ToList();

        if (permission == null)
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        return Ok(new { success = true, permission = permission });
    }
}

