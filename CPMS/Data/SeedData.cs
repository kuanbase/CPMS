using CPMS.Models;
using CPMS.Services;
using CPMS.Types;
using MongoDB.Driver;

namespace CPMS.Data
{
    public class SeedData
    {
        private readonly MongoDBService _mongoDBService;
        private Dictionary<string, string> _NameToId;
        private Dictionary<string, string> _NameToNumber;

        public SeedData(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
            _NameToId = new Dictionary<string, string>();
            _NameToNumber = new Dictionary<string, string>();
        }

        public void Initialize()
        {
            //InitializePremissionData();
            InitializeRoleData();
            InitializeUserData();
        }

        protected void InitializePremissionData()
        {
            // 權限根據職位的行為去劃分
            var permission = _mongoDBService.GetCollection<Permissions>("Permissions");

            if (permission.CountDocuments(Builders<Permissions>.Filter.Empty) == 0)
            {
                TablePermission(ref permission);
                UserPermission(ref permission);

                var all = new Permissions
                {
                    Name = "ALL",
                    Description = "ALL",
                };

                permission.InsertOne(all);

                _NameToId["ALL"] = all.Id!;
            }
        }

        protected void TablePermission(ref IMongoCollection<Permissions> premission)
        {
            var openTable = new Permissions
            {
                Name = "Open Table",
                Description = "Open Table"
            };

            premission.InsertOne(openTable);

            _NameToId["Open Table"] = openTable.Id!;

            var closeTable = new Permissions
            {
                Name = "Close Table",
                Description = "Close Table",
            };

            premission.InsertOne(closeTable);

            _NameToId["Close Table"] = closeTable.Id!;

            var newTable = new Permissions
            {
                Name = "New Table",
                Description = "Close Table",
            };

            premission.InsertOne(newTable);

            _NameToId["New Table"] = newTable.Id!;

            var removeTable = new Permissions
            {
                Name = "Remove Table",
                Description = "Remove Table",
            };

            premission.InsertOne(removeTable);

            _NameToId["Remove Table"] = removeTable.Id!;

            var modifyTable = new Permissions
            {
                Name = "Modify Table",
                Description = "Modify Table",
            };

            premission.InsertOne(modifyTable);

            _NameToId["Modify Table"] = modifyTable.Id!;
        }

        protected void UserPermission(ref IMongoCollection<Permissions> permissiom)
        {
            var newUser = new Permissions
            {
                Name = "New User",
                Description = "New User",
            };

            permissiom.InsertOne(newUser);

            _NameToId["New User"] = newUser.Id!;

            var removeUser = new Permissions
            {
                Name = "Remove User",
                Description = "Remove User",
            };

            permissiom.InsertOne(removeUser);

            _NameToId["Remove User"] = removeUser.Id!;

            var modifyUser = new Permissions
            {
                Name = "Modify User",
                Description = "Modify User"
            };

            permissiom.InsertOne(modifyUser);

            _NameToId["Modify User"] = modifyUser.Id!;

            var queryUser = new Permissions
            {
                Name = "Query User",
                Description = "Query User"
            };

            permissiom.InsertOne(queryUser);

            _NameToId["Query User"] = queryUser.Id!;

            var ownerUser = new Permissions
            {
                Name = "Owner User",
                Description = "Owner User"
            };

            permissiom.InsertOne(ownerUser);

            _NameToId["Owner User"] = ownerUser.Id!;
        }

        protected void InitializeRoleData()
        {
            var role = _mongoDBService.GetCollection<Roles>("Roles");

            if (role.CountDocuments(Builders<Roles>.Filter.Empty) == 0)
            {
                TableRole(ref role);
                UserRole(ref role);

                var admin = new Roles
                {
                    Name = "ADMINISTRATOR",
                };

                role.InsertOne(admin);

                _NameToId["ADMINISTRATOR"] = admin.Id!;
            }
        }

        protected void TableRole(ref IMongoCollection<Roles> role)
        {
            var dealerRole = new Roles
            {
                Name = "Dealer",
            };

            role.InsertOne(dealerRole);

            _NameToId["Dealer"] = dealerRole.Id!;

            var tableManagerRole = new Roles
            {
                Name = "Table Manager",
            };

            role.InsertOne(tableManagerRole);

            _NameToId["Table Manager"] = tableManagerRole.Id!;
        }

        protected void UserRole(ref IMongoCollection<Roles> role)
        {
            var UserManager = new Roles
            {
                Name = "User Manager",
            };

            role.InsertOne(UserManager);

            _NameToId["User Manager"] = UserManager.Id!;
        }

        protected void InitializeUserData()
        {
            var user = _mongoDBService.GetCollection<Users>("Users");

            if (user.CountDocuments(Builders<Users>.Filter.Empty) == 0)
            {
                var admin = new Users
                {
                    Name = "Admin",
                    Number = Users.GetUNumber(),
                    Password = "Admin@12345",
                    Email = "admin@city.com",
                    RoleId = _NameToId["ADMINISTRATOR"],
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };

                user.InsertOne(admin);

                _NameToId["ADMIN"] = admin.Id!;

                var defaultDealer = new Users
                {
                    Name = "Default Dealer",
                    Number = Users.GetUNumber(),
                    Password = "Dealer@12345",
                    Email = "dealer@city.com",
                    RoleId = _NameToId["Dealer"],
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                };

                user.InsertOne(defaultDealer);

                _NameToId["DefaultDealer"] = defaultDealer.Id!;

                var defaultUserManager = new Users
                {
                    Name = "Default User Manager",
                    Number = Users.GetUNumber(),
                    Email = "user@manager.com",
                    Password = "User@12345",
                    RoleId = _NameToId["User Manager"],
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                };

                user.InsertOne(defaultUserManager);

                _NameToId["defaultUserManager"] = defaultUserManager.Id!;

                var defaultTableManager = new Users
                {
                    Name = "Default Table Manager",
                    Number = Users.GetUNumber(),
                    Email = "table@manager.com",
                    Password = "table@12345",
                    RoleId = _NameToId["Table Manager"],
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                };
                
                _NameToId["defaultTableManager"] = defaultTableManager.Id!;
                
                user.InsertOne(defaultTableManager);
            }
        }
    }
}
