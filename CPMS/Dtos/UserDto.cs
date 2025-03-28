using CPMS.Models;
using MongoDB.Driver;

namespace CPMS.Dtos
{
    public class UserDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public uint? Number { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Role { get; set; }
        public bool isLogin { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}
