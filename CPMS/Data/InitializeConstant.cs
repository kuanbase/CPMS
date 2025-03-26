using CPMS.Models;
using CPMS.Services;
using MongoDB.Driver;

namespace CPMS.Data
{
    public class InitializeConstant
    {
        private readonly IMongoCollection<Users> _users;

        public InitializeConstant(MongoDBService mongoDBService)
        {
            _users = mongoDBService.GetCollection<Users>("Users");
        }

        public void Initialize()
        {
            Users.UNumber = (uint)_users.Find(_ => true).ToList().Count;
        }
    }
}
