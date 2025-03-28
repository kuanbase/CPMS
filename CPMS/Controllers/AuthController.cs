using CPMS.Models;
using CPMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace CPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly IMongoCollection<Users> _users;
        private readonly IMongoCollection<Permissions> _premissions;
        private readonly IMongoCollection<Roles> _roles;

        public AuthController(JwtService jwtService, MongoDBService mongoDBService)
        {
            _jwtService = jwtService;
            _users = mongoDBService.GetCollection<Users>("Users");
            _premissions = mongoDBService.GetCollection<Permissions>("Premissions");
            _roles = mongoDBService.GetCollection<Roles>("Roles");
        }

        [HttpPost("GetToken")]
        public IActionResult GetToken([FromBody] LoginRequest request)
        {
            if (request == null || request.Password == string.Empty || request.Password == null || request.Email == string.Empty || request.Email == null)
            {
                return BadRequest(new { success = false, message = "email or password is empty!" });
            }

            var token = _jwtService.Authenticate(request.Email!, request.Password!);

            if (token == null)
            {
                return BadRequest(new { success = false, message = "Invalid email or password" });
            }

            var result = _users.UpdateOne(u => u.Email == request.Email, Builders<Users>.Update.Set(u => u.isLogin, true).Set(u => u.LastLoginDate, DateTime.Now));

            return Ok(new { success = true, token = token, status = "ok"});
        }

        [HttpPost("GetAdminToken")]
        public IActionResult GetAdminToken([FromBody] LoginRequest request)
        {
            if (request == null || request.Password == string.Empty || request.Password == null || request.Email == string.Empty || request.Email == null)
            {
                return BadRequest(new { success = false, message = "email or password is empty!" });
            }

            var token = _jwtService.AdminAuthenticate(request.Email!, request.Password!);

            if (token == null)
            {
                return BadRequest(new { success = false, message = "Invalid email or password" });
            }

            if (token == "")
            {
                return Unauthorized(new { success = false, message = "Permission Denied!" });
            }

            var result = _users.UpdateOne(u => u.Email == request.Email, Builders<Users>.Update.Set(u => u.isLogin, true).Set(u => u.LastLoginDate, DateTime.Now));

            return Ok(new { success = true, token = token, status = "ok" });
        }
    }

    public class LoginRequest
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
