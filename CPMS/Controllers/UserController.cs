using CPMS.Dtos;
using CPMS.Models;
using CPMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Serilog;
using System.Security.Claims;

namespace CPMS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IMongoCollection<Users> _users;
    private readonly IMongoCollection<Roles> _roles;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _ip;
    
    private readonly KafkaLogWriterService _kafkaLogWriterService;
    
    public UserController(MongoDBService mongoDBService, IHttpContextAccessor httpContextAccessor, KafkaLogWriterService kafkaLogWriterService)
    {
        _users = mongoDBService.GetCollection<Users>("Users");
        _roles = mongoDBService.GetCollection<Roles>("Roles");
        _httpContextAccessor = httpContextAccessor;
        _ip = httpContextAccessor.HttpContext!.Connection.RemoteIpAddress!.ToString();
        _kafkaLogWriterService = kafkaLogWriterService;
    }

    private string getName()
    {
        try
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            if (name == null)
            {
                throw new ApplicationException("name is empty!");
            }
            
            return name;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private string getRole()
    {
        try
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == null)
            {
                throw new ApplicationException("role is empty!");
            }

            return role;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private string getId()
    {
        try
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (id == null)
            {
                throw new ApplicationException("id is empty!");
            }

            return id;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private bool HasPermission(string role)
    {
        if (role != "User Manager" && role != "ADMINISTRATOR")
        {
            return false;
        }

        return true;
    }

    [HttpGet("GetUserList")]
    public async Task<IActionResult> GetUserList()
    {
        try
        {
            var role = getRole();

            if (HasPermission(role))
            {
                return Unauthorized(new { success = false, message = "Permission denied!" });
            }

            var name = getName();
            
            var userList = _users.Find(_ => true).ToList();

            await _kafkaLogWriterService.WriteToKafkaLog($"獲取用戶列表成功, 操作人: {name}, IP Address: {_ip}");

            return Ok(new { success = true, data = userList });
        }
        catch (Exception ex)
        {
            Log.Error($"在獲取用戶列表時發生錯誤 - {ex.Message} - IP Address: {_ip}");
            // await _kafkaLogWriterService.WriteToKafkaLog($"在獲取用戶列表時發生錯誤 - {ex.Message} - IP Address: {_ip}");
            return StatusCode(500, new { success = false, message = $"在獲取用戶列表時發生錯誤 - {ex.Message}" });
        }
    }

    [HttpGet("GetUserByOwner")]
    public async Task<IActionResult> GetUserByOwner()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return BadRequest(new { success = false, message = "userId can't be null" });
            }

            var user = await (await _users.FindAsync(u => u.Id == userId)).FirstOrDefaultAsync();
            if (user == null)
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 試圖尋找一個不存在的用戶{userId}");
                return BadRequest(new { success = false, message = $"{_ip} 試圖尋找一個不存在的用戶{userId}" });
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

            return Ok(new { success = true, data = userDto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"");
        }
    }

    [HttpGet("GetUserByName")]
    public async Task<IActionResult> GetUserByName([FromBody] string name)
    {
        try
        {
            var role = getRole();

            if (!HasPermission(role))
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 試圖通過用戶名獲取用戶資料失敗, 權限不足");
                return Unauthorized(new { success = false, message = "Permission Denied!" });
            }

            var users = _users.Find(u => u.Name == name).ToList();

            var currentName = getName();

            if (users == null)
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 試圖尋找一個不存在的用戶{name}, 操作人: {currentName}");
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
                }
                else
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

            await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 獲取用戶資料成功, 操作人: {name}");
            
            return Ok(new { success = true, data = DtoList });
        }
        catch (Exception ex)
        {
            Log.Error($"{_ip} 通過名字獲取用戶時發生異常, {ex.Message}");
            return StatusCode(500, $"{_ip} 通過名字獲取用戶時發生異常, {ex.Message}");
        }
    }

    [HttpGet("GetUserById")]
    public IActionResult GetUserById([FromBody] string Id)
    {
        try
        {
            var role = getRole();

            if (!HasPermission(role))
            {
                return Unauthorized(new { success = false, message = $"{_ip} 試圖通過用戶ID獲取用戶失敗, 權限不足" });
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
                isLogin = user.isLogin,
            };

            return Ok(new { success = true, data = userDto });
        }
        catch (Exception ex)
        {
            Log.Error($"{_ip} 獲取用戶時，發生異常");
            return StatusCode(500, $"{_ip} 獲取用戶時，發生異常");
        }
    }

    // [HttpGet("GetUserByTimeArea")]
    // public IActionResult GetUserByTimeArea([FromBody] TimeArea dateArea)
    // {
    //     var role = User.FindFirst(ClaimTypes.Role)?.Value;
    //
    //     if (role == null || role != "User Manager")
    //     {
    //         return Unauthorized(new { success = false, message = "Permission Denied!" });
    //     }
    //
    //     var users = _users.Find(u => u.CreatedDate >= dateArea.StartTime && u.CreatedDate <= dateArea.EndTime).ToList();
    //
    //     if (users == null)
    //     {
    //         return Ok(new { success = true, message = "區間內沒有用戶" });
    //     }
    //
    //     return Ok(new { });
    // }

    [HttpGet("GetListByCurrentIndex")]
    public async Task<IActionResult> GetListByCurrentIndex([FromQuery] int page, [FromQuery] int pageSize)
    {
        try
        {
            var name = getName();
            var role = getRole();
            if (!HasPermission(role))
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 試圖獲取用戶列表, 權限不足");
                return Unauthorized(new { success = false, message = "Permission Denied!" });
            }

            if (page <= 0 || pageSize <= 0)
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 試圖獲取用戶列表, 但由於參數設置錯誤，獲取失敗");
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
                    isLogin = user.isLogin,
                    LastLoginDate = user.LastLoginDate,
                };
                userDtos.Add(dto);
            }

            Console.WriteLine(userDtos);

            if (userDtos.Count == 0)
            {
                return BadRequest(new { message = "userDtos is null" });
            }

            await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 獲取用戶列表成功, 操作人: {name}");

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
        catch (Exception ex)
        {
            Log.Error($"{_ip} 獲取用戶列表失敗, 系統異常");
            return StatusCode(500, $"{_ip} 獲取用戶列表失敗, 系統異常");
        }
    }

    [HttpPut("ModifyUserById")]
    public async Task<IActionResult> ModifyUserById([FromBody] UserRequestBody body)
    {
        try
        {
            var role = getRole();

            if (!HasPermission(role))
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 沒有權限但試圖修改用戶資料");
                return Unauthorized(new { success = false, message = "Permission Denied!" });
            }

            var roleFromDB = await _roles.Find(r => r.Name == body.role).FirstOrDefaultAsync();

            if (roleFromDB == null)
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip}試圖找不存在的權限 {body.role}");
                return BadRequest(new { success = false, message = "Role doesn't exist!" });
            }

            var result = await _users.UpdateOneAsync(u => u.Id == body.id,
                Builders<Users>.Update.Set(u => u.Name, body.name)
                    .Set(u => u.Email, body.email)
                    .Set(u => u.Password, body.password)
                    .Set(u => u.RoleId, roleFromDB.Id)
                    .Set(u => u.LastModifiedDate, DateTime.Now)
            );

            if (result.ModifiedCount == 0 && result.MatchedCount > 0)
            {
                return BadRequest(new { success = false, message = "Everything updated already!" });
            }
            else if (result.ModifiedCount <= 0)
            {
                return BadRequest(new { success = false, message = "User doesn't exist!" });
            }

            return Ok(new { success = true, message = "User data update successful!" });
        }
        catch (Exception e)
        {
            Log.Error($"{_ip} 修改用戶資料時發生系統性錯誤 {e.Message}");
            return StatusCode(500, $"{_ip} 修改用戶資料時發生系統性錯誤 {e.Message}");
        }
    }

    [HttpPut("ModifyUserPassowrdByOwner")]
    public async Task<IActionResult> ModifyUserPassowrdByOwner([FromBody] string? password)
    {
        try
        {
            var role = getRole();

            var userId = getId();

            var name = getName();

            var user = _users.Find(u => u.Id == userId).FirstOrDefault();

            if (user == null)
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 查詢的用戶 {userId} 不存在!");
                return BadRequest(new { success = false, message = $"{_ip} 查詢的用戶 {userId} 不存在!" });
            }

            if (password != null || user.Password != password)
            {
                await _users.UpdateOneAsync(u => u.Id == userId, Builders<Users>.Update.Set(u => u.Password, password));

                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 修改密碼成功, 操作人: {name}, 操作人: {name}");
                
                return Ok(new { success = true, message = "Modify password successful!" });
            }

            await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 修改密碼失敗, 輸入了一個空密碼");

            return BadRequest(new { success = false, message = $"{_ip} 修改密碼失敗, 輸入了一個空密碼" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{_ip} 在用戶修改自己資料時，發生異常。");
        }
    }

    [HttpPost("RemoveById")]
    public async Task<IActionResult> RemoveById([FromBody] UserRequestBody idUserRequest)
    {
        try
        {
            var id = idUserRequest.id;

            var role = getRole();

            if (!HasPermission(role))
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"Permission Denied! - IP Address {_ip}");
                return Unauthorized(new { success = false, message = "Permission Denied!", role = role });
            }

            var result = await _users.DeleteOneAsync(u => u.Id == id);

            if (result.DeletedCount <= 0)
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"User doesn't exist! - IP Address {_ip}");
                return BadRequest(new { success = false, message = "User doesn't exist!" });
            }

            var name = getName();

            await _kafkaLogWriterService.WriteToKafkaLog($"成功刪除用戶: {id}, 操作人: {name}, IP Address: {_ip}");
            
            return Ok(new { success = true, message = "Remove user successful!" });
        }
        catch (Exception ex)
        {
            Log.Error($"刪除用戶時發生異常, {ex.Message}, IP Address: {_ip}");
            return StatusCode(500, $"刪除用戶時發生異常, {ex.Message}, IP Address: {_ip}");
        }
        
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRequestBody userRequest)
    {
        try
        {
            var role = getRole();

            if (!HasPermission(role))
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 試圖新增用戶失敗, 權限不足");
                return Unauthorized(new { success = false, message = "Permission denied!" });
            }

            var newUserRole = _roles.Find(r => r.Name == userRequest.role).FirstOrDefault();

            var user = new Users
            {
                Email = userRequest.email,
                Password = userRequest.password,
                Name = userRequest.name,
                Number = Users.GetUNumber(),
                RoleId = newUserRole.Id,
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
            };

            var name = getName();

            await _users.InsertOneAsync(user);

            await _kafkaLogWriterService.WriteToKafkaLog($"注册成功, 操作人: {name}, IP Address: {_ip}");
            
            return Ok(new { success = true, message = "Register successful!" });
        }
        catch (Exception ex)
        {
            Log.Error($"在注册時發生異常, {ex.Message}, IP Address: {_ip}");
            return StatusCode(500, new { success = false, message = $"在注册時發生異常, {ex.Message}, IP Address: {_ip}" });
        }
    }

    [HttpGet("GetOnlineUserList")]
    public async Task<IActionResult> GetOnlineUserList()
    {
        try
        {
            var role = getRole();
            if (!HasPermission(role))
            {
                await _kafkaLogWriterService.WriteToKafkaLog($"{_ip} 試圖獲取用戶列表失敗, 權限不足");
                return Unauthorized(new { success = false, message = "Permission Denied!" });
            }

            var result = await _users.Find(u => u.isLogin).ToListAsync();
            
            await _kafkaLogWriterService.WriteToKafkaLog($"獲取在線用戶列表成功");

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            Log.Error($"在獲取線上用戶列表時, 發生異常, {ex.Message}, IP Address: {_ip}");
            return StatusCode(500, $"在獲取線上用戶列表時，發生異常, {ex.Message}, IP Address: {_ip}");
        }
    }

    [HttpGet("GetUserAmount")]
    public async Task<IActionResult> GetUserAmount()
    {
        try
        {
            var role = getRole();
            if (!HasPermission(role))
            {
                return Unauthorized(new { success = false, message = "Permission Denied!" });
            }

            var name = getName();
            
            var amount = await _users.Find(_ => true).CountDocumentsAsync();

            await _kafkaLogWriterService.WriteToKafkaLog($"獲取用戶數量成功, 操作人: {name}, IP Address: {_ip}");

            return Ok(new { success = true, data = amount });
        }
        catch (Exception ex)
        {
            Log.Fatal($"獲取用戶數量時，發生意外: {ex}, IP Address: {_ip}");
            return StatusCode(500, $"獲取用戶數量時，發生意外: {ex}, IP Address: {_ip}");
        }
    }

    public class IdRequest
    {
        public string? id { get; set; }
    }

    public class UserRequestBody
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
