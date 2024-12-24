using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Interface;

public interface IRoleService
{
    Task<Role> AddRole(Role role);
    Task<Role?> GetRoleById(int id);
    Task<Role?> GetRoleByName(string roleName);
    Task<Role> UpdateRole(Role role);
    Task<List<Role>> GetAllRoles();
}