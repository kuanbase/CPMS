namespace CPMS.Services;

public class DefaultPremissionService : IPermissionService
{
    
    public void AddRolePermission(string role, string permission)
    {
        throw new NotImplementedException();
    }

    public void AssignRoleToUser(string userID, string role)
    {
        throw new NotImplementedException();
    }

    public bool HasPermission(string userID, string permission)
    {
        throw new NotImplementedException();
    }
}
