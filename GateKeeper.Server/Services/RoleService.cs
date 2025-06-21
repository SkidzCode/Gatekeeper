using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper; // Added Dapper namespace

namespace GateKeeper.Server.Services
{
    public class RoleService : IRoleService
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<RoleService> _logger;

        /// <summary>
        /// Constructor for the RoleService.
        /// </summary>
        /// <param name="dbHelper">Database helper for obtaining connections.</param>
        /// <param name="logger">Logger for RoleService.</param>
        public RoleService(
            // IConfiguration configuration, // Removed
            IDbHelper dbHelper,
            ILogger<RoleService> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
            // Retrieve database connection string if needed
            // var dbConfig = configuration.GetSection("DatabaseConfig").Get<DatabaseConfig>() ?? new DatabaseConfig();
        }

        /// <summary>
        /// Inserts a new Role via the InsertRole stored procedure.
        /// </summary>
        /// <param name="role">Role object containing RoleName.</param>
        /// <returns>The inserted Role (with any DB-generated fields, if applicable).</returns>
        public async Task<Role> AddRole(Role role)
        {
            await using var connection = await _dbHelper.GetWrapperAsync();
            // TODO: Refactor this method to use Dapper if it's part of the scope,
            // for now, keeping it as is, assuming it might use methods from IMySqlConnectorWrapper
            // that are not directly replaced by Dapper's typical Query/Execute.
            // If it can be simplified with Dapper's ExecuteAsync, it should be.
            await connection.ExecuteNonQueryAsync("InsertRole", CommandType.StoredProcedure,
                new MySqlParameter("@p_RoleName", MySqlDbType.VarChar, 50) { Value = role.RoleName });

            // If your SP sets any output parameters, retrieve them here. Example if InsertRole returns last inserted Id:
            // role.Id = Convert.ToInt32(cmd.Parameters["@last_id"].Value);

            return role;
        }

        /// <summary>
        /// Gets a single Role by Id via the GetRoleById stored procedure.
        /// </summary>
        /// <param name="id">Unique Id of the Role.</param>
        /// <returns>Role object or null if not found.</returns>
        public async Task<Role?> GetRoleById(int id)
        {
            // Role? role = null; // No longer needed with Dapper like this

            await using var connection = await _dbHelper.GetConnectionAsync(); // Assuming GetConnectionAsync returns a raw DbConnection
            // await using var reader = await connection.ExecuteReaderAsync("GetRoleById", CommandType.StoredProcedure,
            //     new MySqlParameter("@p_Id", MySqlDbType.Int32) { Value = id });

            // if (await reader.ReadAsync())
            // {
            //     role = new Role()
            //     {
            //         Id = Convert.ToInt32(reader["Id"]),
            //         RoleName = reader["RoleName"].ToString() ?? string.Empty
            //     };
            // }

            // return role;
            // TODO: Refactor this method to use Dapper.
            // This requires changing _dbHelper.GetWrapperAsync() to return a DbConnection
            // or modifying IMySqlConnectorWrapper to expose a DbConnection.
            // For now, leaving it as is to focus on GetAllRoles and GetRoleByName first.
            // Placeholder for Dapper implementation:
            // return await connection.QueryFirstOrDefaultAsync<Role>("GetRoleById", new { p_Id = id }, commandType: CommandType.StoredProcedure);

            // Temporary: keeping old implementation until IDbHelper is clarified for Dapper usage
            Role? role = null;
            await using var wrapper = await _dbHelper.GetWrapperAsync();
            await using var reader = await wrapper.ExecuteReaderAsync("GetRoleById", CommandType.StoredProcedure,
                new MySqlParameter("@p_Id", MySqlDbType.Int32) { Value = id });
            if (await reader.ReadAsync())
            {
                role = new Role()
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    RoleName = reader["RoleName"].ToString() ?? string.Empty
                };
            }
            return role;
        }

        /// <summary>
        /// Gets a single Role by name via the GetRoleByName stored procedure.
        /// </summary>
        /// <param name="roleName">Name of the role.</param>
        /// <returns>Role object or null if not found.</returns>
        public async Task<Role?> GetRoleByName(string roleName)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();
            var connection = wrapper.GetDbConnection();

            var role = await connection.QueryFirstOrDefaultAsync<Role>(
                "GetRoleByName",
                new { p_RoleName = roleName }, // Dapper uses anonymous objects for parameters
                commandType: CommandType.StoredProcedure);

            return role;
        }

        /// <summary>
        /// Updates a Role via the UpdateRole stored procedure.
        /// </summary>
        /// <param name="role">Role object containing Id and (optionally) a new RoleName.</param>
        /// <returns>The updated Role.</returns>
        public async Task<Role> UpdateRole(Role role)
        {
            await using var connection = await _dbHelper.GetWrapperAsync();
            // TODO: Refactor this method to use Dapper if it's part of the scope.
            await connection.ExecuteNonQueryAsync("UpdateRole", CommandType.StoredProcedure,
                new MySqlParameter("@p_Id", MySqlDbType.Int32) { Value = role.Id },
                new MySqlParameter("@p_RoleName", MySqlDbType.VarChar, 50) { Value = role.RoleName });

            return role;
        }

        /// <summary>
        /// Retrieves all Roles via the GetAllRoles stored procedure.
        /// </summary>
        /// <returns>List of Role objects.</returns>
        public async Task<List<Role>> GetAllRoles()
        {
            // var roles = new List<Role>(); // No longer needed

            await using var wrapper = await _dbHelper.GetWrapperAsync();
            var connection = wrapper.GetDbConnection();
            // await using var reader = await connection.ExecuteReaderAsync("GetAllRoles", CommandType.StoredProcedure);

            // while (await reader.ReadAsync())
            // {
            //     var role = new Role()
            //     {
            //         Id = Convert.ToInt32(reader["Id"]),
            //         RoleName = reader["RoleName"].ToString() ?? string.Empty
            //     };
            //     roles.Add(role);
            // }

            // return roles;
            var roles = await connection.QueryAsync<Role>("GetAllRoles", commandType: CommandType.StoredProcedure);
            return roles.ToList();
        }
    }
}