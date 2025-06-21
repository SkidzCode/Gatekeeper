using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IDbConnection _dbConnection;

        public RoleRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<Role> AddRoleAsync(Role role)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_RoleName", role.RoleName, DbType.String, ParameterDirection.Input, 50);

            var newRoleId = await _dbConnection.QuerySingleAsync<int>("InsertRole", parameters, commandType: CommandType.StoredProcedure);
            role.Id = newRoleId;
            return role;
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", id, DbType.Int32);

            return await _dbConnection.QueryFirstOrDefaultAsync<Role>("GetRoleById", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_RoleName", roleName, DbType.String, ParameterDirection.Input, 50);

            return await _dbConnection.QueryFirstOrDefaultAsync<Role>("GetRoleByName", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<Role> UpdateRoleAsync(Role role)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", role.Id, DbType.Int32);
            parameters.Add("@p_RoleName", role.RoleName, DbType.String, ParameterDirection.Input, 50);

            await _dbConnection.ExecuteAsync("UpdateRole", parameters, commandType: CommandType.StoredProcedure);
            return role;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            var roles = await _dbConnection.QueryAsync<Role>("GetAllRoles", commandType: CommandType.StoredProcedure);
            return roles.ToList();
        }
    }
}
