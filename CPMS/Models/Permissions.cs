using CPMS.Types;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CPMS.Models
{
    public class Permissions
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
