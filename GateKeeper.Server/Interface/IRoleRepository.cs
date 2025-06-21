using GateKeeper.Server.Models.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface IRoleRepository
    {
        Task<Role> AddRoleAsync(Role role);
        Task<Role?> GetRoleByIdAsync(int id);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task<Role> UpdateRoleAsync(Role role);
        Task<List<Role>> GetAllRolesAsync();
    }
}
