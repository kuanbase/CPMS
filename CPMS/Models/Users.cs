namespace CPMS.Models;

using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Formats.Asn1;

public class Users
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public uint? Number { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RoleId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    public static uint UNumber = 0;

    public bool isLogin { get; set; }
    public DateTime? LastLoginDate { get; set; }

    public static uint GetUNumber()
    {
        UNumber += 1;
        return UNumber;
    }
}
