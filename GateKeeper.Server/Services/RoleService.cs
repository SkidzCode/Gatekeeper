using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
            Role? role = null;

            await using var connection = await _dbHelper.GetWrapperAsync();
            await using var reader = await connection.ExecuteReaderAsync("GetRoleById", CommandType.StoredProcedure,
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
            Role? role = null;

            await using var connection = await _dbHelper.GetWrapperAsync();
            await using var reader = await connection.ExecuteReaderAsync("GetRoleByName", CommandType.StoredProcedure,
                new MySqlParameter("@p_RoleName", MySqlDbType.VarChar, 50) { Value = roleName });

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
        /// Updates a Role via the UpdateRole stored procedure.
        /// </summary>
        /// <param name="role">Role object containing Id and (optionally) a new RoleName.</param>
        /// <returns>The updated Role.</returns>
        public async Task<Role> UpdateRole(Role role)
        {
            await using var connection = await _dbHelper.GetWrapperAsync();
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
            var roles = new List<Role>();

            await using var connection = await _dbHelper.GetWrapperAsync();
            await using var reader = await connection.ExecuteReaderAsync("GetAllRoles", CommandType.StoredProcedure);

            while (await reader.ReadAsync())
            {
                var role = new Role()
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    RoleName = reader["RoleName"].ToString() ?? string.Empty
                };
                roles.Add(role);
            }

            return roles;
        }
    }
}