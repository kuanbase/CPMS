using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using CPMS.Models;

namespace CPMS.Services;

public class JwtService
{
    private readonly IConfiguration _config;
    private readonly IMongoCollection<Users> _usersCollection;
    private readonly IMongoCollection<Roles> _rolesCollection;
    private readonly IMongoCollection<Permissions> _permissionsCollection;

    public JwtService(IConfiguration config, MongoDBService database)
    {
        _config = config;
        _usersCollection = database.GetCollection<Users>("Users");
        _rolesCollection = database.GetCollection<Roles>("Roles");
        _permissionsCollection = database.GetCollection<Permissions>("Permissions");
    }

    public string? Authenticate(string email, string password)
    {
        // 1. 查找用户
        var user = _usersCollection.Find(u => u.Email == email && u.Password == password).FirstOrDefault();
        if (user == null) return null; // 用户不存在或密码错误

        // 2. 查找角色
        var role = _rolesCollection.Find(r => r.Id == user.RoleId).FirstOrDefault();
        var roleName = role?.Name ?? "Guest";

        // 3. 查找权限
        //var permissions = _permissionsCollection
        //    .Find(p => role!.PermissionIds!.Contains(p.Id!))
        //    .Project(p => p.Name)
        //    .ToList();

        // 4. 生成 JWT 令牌
        return GenerateToken(user, roleName);
    }

    private string GenerateToken(Users user, string role)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"]!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.Name!),
            new Claim(ClaimTypes.Role, role),
            //new Claim("Permissions", string.Join(",", permissions))
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(secretKey),
                SecurityAlgorithms.HmacSha256Signature
            )
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
