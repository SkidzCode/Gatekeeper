using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;

namespace GateKeeper.Server.Services
{
    public class RoleService : IRoleService
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<RoleService> _logger;

        /// <summary>
        /// Constructor for the RoleService.
        /// </summary>
        /// <param name="dbConnection">Database connection.</param>
        /// <param name="logger">Logger for RoleService.</param>
        public RoleService(
            IDbConnection dbConnection,
            ILogger<RoleService> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        /// <summary>
        /// Inserts a new Role via the InsertRole stored procedure.
        /// </summary>
        /// <param name="role">Role object containing RoleName.</param>
        /// <returns>The inserted Role (with any DB-generated fields, if applicable).</returns>
        public async Task<Role> AddRole(Role role)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_RoleName", role.RoleName, DbType.String, ParameterDirection.Input, 50);

            var newRoleId = await _dbConnection.QuerySingleAsync<int>("InsertRole", parameters, commandType: CommandType.StoredProcedure);
            role.Id = newRoleId;
            return role;
        }

        /// <summary>
        /// Gets a single Role by Id via the GetRoleById stored procedure.
        /// </summary>
        /// <param name="id">Unique Id of the Role.</param>
        /// <returns>Role object or null if not found.</returns>
        public async Task<Role?> GetRoleById(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", id, DbType.Int32);

            return await _dbConnection.QueryFirstOrDefaultAsync<Role>("GetRoleById", parameters, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Gets a single Role by name via the GetRoleByName stored procedure.
        /// </summary>
        /// <param name="roleName">Name of the role.</param>
        /// <returns>Role object or null if not found.</returns>
        public async Task<Role?> GetRoleByName(string roleName)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_RoleName", roleName, DbType.String, ParameterDirection.Input, 50);

            return await _dbConnection.QueryFirstOrDefaultAsync<Role>("GetRoleByName", parameters, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Updates a Role via the UpdateRole stored procedure.
        /// </summary>
        /// <param name="role">Role object containing Id and (optionally) a new RoleName.</param>
        /// <returns>The updated Role.</returns>
        public async Task<Role> UpdateRole(Role role)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", role.Id, DbType.Int32);
            parameters.Add("@p_RoleName", role.RoleName, DbType.String, ParameterDirection.Input, 50);

            await _dbConnection.ExecuteAsync("UpdateRole", parameters, commandType: CommandType.StoredProcedure);
            return role;
        }

        /// <summary>
        /// Retrieves all Roles via the GetAllRoles stored procedure.
        /// </summary>
        /// <returns>List of Role objects.</returns>
        public async Task<List<Role>> GetAllRoles()
        {
            var roles = await _dbConnection.QueryAsync<Role>("GetAllRoles", commandType: CommandType.StoredProcedure);
            return roles.ToList();
        }
    }
}