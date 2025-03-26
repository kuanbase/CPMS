using CPMS.Dtos;
using CPMS.Models;
using CPMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Security.Claims;

namespace CPMS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IMongoCollection<Users> _users;
    private readonly IMongoCollection<Roles> _roles;

    public UserController(MongoDBService mongoDBService)
    {
        _users = mongoDBService.GetCollection<Users>("Users");
        _roles = mongoDBService.GetCollection<Roles>("Roles");
    }

    [HttpGet("GetUserList")]
    public IActionResult GetUserList()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || (role != "User Manager" && role != "ADMINISTRATOR"))
        {
            return Unauthorized(new { success = false, message = "Permission denied!" });
        }

        var userList = _users.Find(_ => true).ToList();

        return Ok(new { success = true, data = userList });
    }

    [HttpGet("GetUserByOwner")]
    public async Task<IActionResult> GetUserByOwner()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return BadRequest(new { success = false, message = "userId can't be null" });
        }

        var user = await (await _users.FindAsync(u => u.Id == userId)).FirstOrDefaultAsync();
        if (user == null)
        {
            return BadRequest(new { success = false, message = "user account does not found" });
        }

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == null)
        {
            return Problem("Internal Server Error", statusCode: 500);
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Number = user.Number,
            Role = role,
            Password = user.Password,
            CreatedDate = user.CreatedDate,
        };

        return Ok(new { success = true, data = userDto, ssc = userDto, fuck = userDto });
    }

    [HttpGet("GetUserByName")]
    public IActionResult GetUserByName([FromBody] string name)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role == "User Manager")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var users = _users.Find(u => u.Name == name).ToList();

        if (users == null)
        {
            return BadRequest(new { success = false, message = "No one name " + name });
        }

        var DtoList = new List<UserDto>();

        foreach (var user in users)
        {
            var roleFromDb = _roles.Find(r => r.Id == user.RoleId).FirstOrDefault();

            string roleName = string.Empty;

            if (roleFromDb == null)
            {
                roleName = "Permission Miss";
            } else
            {
                roleName = roleFromDb.Name!;
            }

            var userDto = new UserDto
            {
                Name = user.Name,
                Email = user.Email,
                Password = user.Password,
                Number = user.Number,
                CreatedDate = user.CreatedDate,
                Role = roleName,
            };

            DtoList.Add(userDto);
        }

        return Ok(new { success = true, data = DtoList });
    }

    [HttpGet("GetUserById")]
    public IActionResult GetUserById([FromBody] string Id)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "User Manager")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var user = _users.Find(u => u.Id == Id).FirstOrDefault();

        if (user == null)
        {
            return BadRequest(new { success = false, message = "The user account doesn't exist!" });
        }

        var roleFromDb = _roles.Find(r => r.Id == user.RoleId).FirstOrDefault();
        var roleName = string.Empty;

        if (roleFromDb != null)
        {
            roleName = roleFromDb.Name;
        }

        var userDto = new UserDto
        {
            Name = user.Name,
            Email = user.Email,
            Password = user.Password,
            Number = user.Number,
            CreatedDate = user.CreatedDate,
            Role = roleName,
        };

        return Ok(new { success = true, data = userDto });
    }

    [HttpGet("GetUserByTimeArea")]
    public IActionResult GetUserByTimeArea([FromBody] TimeArea dateArea)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || role != "User Manager")
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var users = _users.Find(u => u.CreatedDate >= dateArea.StartTime && u.CreatedDate <= dateArea.EndTime).ToList();

        if (users == null)
        {
            return Ok(new { success = true, message = "區間內沒有用戶" });
        }

        return Ok(new { });
    }

    [HttpGet("GetListByCurrentIndex")]
    public async Task<IActionResult> GetListByCurrentIndex([FromQuery] int page, [FromQuery] int pageSize)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == null || (role != "User Manager" && role != "ADMINISTRATOR"))
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { success = false, message = "Page and PageSize must be greater than zero!" });
        }

        // 獲取user總數
        var total = await _users.CountDocumentsAsync(_ => true);

        // 分頁查詢
        var users = await _users.Find(_ => true)
                                .Skip((page - 1) * pageSize)
                                .Limit(pageSize)
                                .ToListAsync();

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roleFromDB = await _roles.Find(r => r.Id == user.RoleId).FirstOrDefaultAsync();

            if (roleFromDB == null)
            {
                Console.WriteLine("FUCK YOU MOTHER !!!!");
            }

            var dto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Number = user.Number,
                Email = user.Email,
                Password = user.Password,
                CreatedDate = user.CreatedDate,
                Role = roleFromDB!.Name,
            };
            userDtos.Add(dto);
        }

        Console.WriteLine(userDtos);

        if (userDtos.Count == 0)
        {
            return BadRequest(new { message = "userDtos is null" });
        }

        return Ok(new
        {
            success = true,
            data = userDtos,
            ssc = userDtos,
            total = total,
            page = page,
            pageSize = pageSize
        });
    }

    [HttpPut("ModifyUserById")]
    public async Task<IActionResult> ModifyUserById([FromBody] RequestBody body)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || (role != "User Manager" && role != "ADMINISTRATOR"))
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var roleFromDB = await _roles.Find(r => r.Name == body.role).FirstOrDefaultAsync();

        if (roleFromDB == null)
        {
            return BadRequest(new { success = false, message = "Role doesn't exist!" });
        }

        var result = _users.UpdateOne(u => u.Id == body.id, 
            Builders<Users>.Update.Set(u => u.Name, body.name)
            .Set(u => u.Email, body.email)
            .Set(u => u.Password, body.password)
            .Set(u => u.RoleId, roleFromDB.Id)
            );

        if (result.ModifiedCount == 0 && result.MatchedCount > 0)
        {
            return BadRequest(new { success = false, message = "Everything updated already!" });
        } else if (result.ModifiedCount <= 0)
        {
            return BadRequest(new { success = false, message = "User doesn't exist!" });
        }

        return Ok(new { success = true, message = "User data update successful!" });
    }

    [HttpPut("ModifyUserPassowrdByOwner")]
    public IActionResult ModifyUserPassowrdByOwner([FromBody] string password)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null)
        {
            return Unauthorized(new { success = false, message = "Permission Denied!" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return BadRequest(new { success = false, message = "User Id Not Found!" });
        }

        var user = _users.Find(u => u.Id == userId).FirstOrDefault();

        if (user == null)
        {
            return BadRequest(new { success = false, message = "User Account Doesn't Exsit!" });
        }

        if (password != null || user.Password != password)
        {
            user.Password = password;
            _users.ReplaceOne(u => u.Id == user.Id, user);

            return Ok(new { success = true, message = "Modify password successful!" });
        }

        return Ok(new { success = false, message = "New password can't be null!" });
    }

    [HttpPost("RemoveById")]
    public async Task<IActionResult> RemoveById([FromBody] RequestBody idRequest)
    {

        var id = idRequest.id;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || (role != "User Manager" && role != "ADMINISTRATOR"))
        {
            return Unauthorized(new { success = false, message = "Permission Denied!", role = role });
        }

        var result = await _users.DeleteOneAsync(u => u.Id == id);

        if (result.DeletedCount <= 0)
        {
            return BadRequest(new { success = false, message = "User doesn't exist!" });
        }

        return Ok(new { success = true, message = "Remove user successful!" });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RequestBody request)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == null || (role != "User Manager" && role != "ADMINISTRATOR"))
        {
            return Unauthorized(new { success = false, message = "Permission denied!" });
        }

        var newUserRole = _roles.Find(r => r.Name == request.role).FirstOrDefault();

        var user = new Users
        {
            Email = request.email,
            Password = request.password,
            Name = request.name,
            Number = Users.GetUNumber(),
            RoleId = newUserRole.Id,
            CreatedDate = DateTime.Now,
            LastModifiedDate = DateTime.Now,
        };

        _users.InsertOne(user);

        return Ok(new { success = true, message = "Register successful!" });
    }

    public class IdRequest
    {
        public string? id { get; set; }
    }

    public class RequestBody
    {
        public string? id { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public string? name { get; set; }
        public string? role { get; set; }
    }

    public class RegisterRequest
    {
        public string? email { get; set; }
        public string? password { get; set; }
        public string? name { get; set; }
        public string? role { get; set; }
    }

    public class TimeArea
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class Pagination
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }
}
