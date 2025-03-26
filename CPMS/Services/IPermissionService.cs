using System.Data;

namespace CPMS.Services;

public interface IPermissionService
{
    // 檢查用戶是否具有某個權限
    bool HasPermission(string userID, string permission);

    // 添加角色權限
    void AddRolePermission(string role, string permission);

    // 給用戶分配角色
    void AssignRoleToUser(string userID, string role);
}
