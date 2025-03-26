using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CPMS.Models
{
    public class Roles
    {
        // 這個權限角色用最簡單的方式去完成。
        // 總的來說，只有兩種角色。一是管理層員工，二是底層員工。
        // 根據工作內容，又可以分為User，Form，Game。
        // 為了簡化一點，我們把Game Manager， 統一稱為Table Manager，然後還有Pits，Dealer。
        // Employee Manager
        // Finance Manager

        // 之外，其實每一張台還需要補充籌碼，因此我們需要引入一個籌碼管理員，這是一個非常大的權限。
        // 由Pits負責決定要不要加碼在該台上。
        
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Name { get; set; }
        //[BsonRepresentation(BsonType.ObjectId)]
        //public List<string>? PermissionIds { get; set; }
    }
}
