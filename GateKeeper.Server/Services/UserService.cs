using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using GateKeeper.Server.Controllers;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Services;

public class UserService : IUserService
{
    private readonly IDBHelper _dbHelper;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Constructor for the UserController.
    /// </summary>
    /// <param name="configuration">Application configuration dependency.</param>
    public UserService(IConfiguration configuration, IDBHelper dbHelper, ILogger<UserService> logger)
    {
        // Retrieve database connection string
        var dbConfig = configuration.GetSection("DatabaseConfig").Get<DatabaseConfig>() ?? new DatabaseConfig();
        _dbHelper = dbHelper;
        _logger = logger;
    }

    /// <summary>
    /// API endpoint to add/register a new user.
    /// </summary>
    /// <param name="user">User object containing registration details.</param>
    /// <returns>HTTP response indicating success or failure.</returns>
    public async Task<(int, User)> AddUser(User user)
    {
        // Step 1: Generate a unique salt and hash the user's password
        var salt = PasswordHelper.GenerateSalt();
        var hashedPassword = PasswordHelper.HashPassword(user.Password, salt);

        // Step 2: Establish database connection
        await using var connection = await _dbHelper.GetOpenConnectionAsync();

        // Step 3: Create and execute a stored procedure command
        await using var cmd = new MySqlCommand("AddUser", connection);
        cmd.CommandType = System.Data.CommandType.StoredProcedure;

        // Add parameters to match the stored procedure inputs
        cmd.Parameters.Add(new MySqlParameter("@p_FirstName", MySqlDbType.VarChar, 50)).Value = user.FirstName;
        cmd.Parameters.Add(new MySqlParameter("@p_LastName", MySqlDbType.VarChar, 50)).Value = user.LastName;
        cmd.Parameters.Add(new MySqlParameter("@p_Email", MySqlDbType.VarChar, 100)).Value = user.Email;
        cmd.Parameters.Add(new MySqlParameter("@p_Username", MySqlDbType.VarChar, 50)).Value = user.Username;
        cmd.Parameters.Add(new MySqlParameter("@p_Password", MySqlDbType.VarChar, 255)).Value = hashedPassword;
        cmd.Parameters.Add(new MySqlParameter("@p_Salt", MySqlDbType.VarChar, 255)).Value = salt;
        cmd.Parameters.Add(new MySqlParameter("@p_Phone", MySqlDbType.VarChar, 15)).Value = user.Phone;

        // Add output parameter for result code
        cmd.Parameters.Add(new MySqlParameter("@p_ResultCode", MySqlDbType.Int32) { Direction = ParameterDirection.Output });
        cmd.Parameters.Add(new MySqlParameter("last_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output });

        await cmd.ExecuteNonQueryAsync();

        var resultCode = (int)cmd.Parameters["@p_ResultCode"].Value;
        var userId = 0;
        if (cmd.Parameters["@last_id"].Value != DBNull.Value)
        {
            user.Id = Convert.ToInt32(cmd.Parameters["@last_id"].Value);
        }
        return (resultCode, user);
    }

    public async Task<int> ChangePassword(int userId, string newPassword)
    {
        await using var connection = await _dbHelper.GetOpenConnectionAsync();
        await using var cmd = new MySqlCommand("PasswordChange", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        var salt = PasswordHelper.GenerateSalt();
        var hashedPassword = PasswordHelper.HashPassword(newPassword, salt);
        cmd.Parameters.AddWithValue("@p_HashedPassword", hashedPassword);
        cmd.Parameters.AddWithValue("@p_Salt", salt);
        cmd.Parameters.AddWithValue("@p_UserId", userId);

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<string>> GetRolesAsync(int Id)
    {
        await using var connection = await _dbHelper.GetOpenConnectionAsync();

        await using var cmd = new MySqlCommand("GetUserRoles", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@p_UserId", Id);

        var roles = new List<string>();

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) // Loop through all rows
        {
            var roleName = reader["RoleName"]?.ToString();
            if (!string.IsNullOrEmpty(roleName))
            {
                roles.Add(roleName);
            }
        }

        return roles;
    }


    public async Task<User?> GetUser(string identifier)
    {
        await using var connection = await _dbHelper.GetOpenConnectionAsync();
        User? user = null;

        await using var cmd = new MySqlCommand("GetUserProfileByIdentifier", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@p_Identifier", identifier);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            user = new User()
            {
                Id = Convert.ToInt32(reader["Id"]),
                FirstName = reader["FirstName"].ToString() ?? string.Empty,
                LastName = reader["LastName"].ToString() ?? string.Empty,
                Email = reader["Email"].ToString() ?? string.Empty,
                Phone = reader["Phone"].ToString() ?? string.Empty,
                Salt = reader["Salt"].ToString() ?? string.Empty,
                Password = reader["Password"].ToString() ?? string.Empty,
                Username = reader["Username"].ToString() ?? string.Empty
            };
        }

        user.Roles = await GetRolesAsync(user.Id);

        return user;
    }

    public async Task<User?> GetUser(int identifier)
    {
        await using var connection = await _dbHelper.GetOpenConnectionAsync();
        User? user = null;

        await using var cmd = new MySqlCommand("GetUser", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@p_UserId", identifier);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            user = new User()
            {
                Id = Convert.ToInt32(reader["Id"]),
                FirstName = reader["FirstName"].ToString() ?? string.Empty,
                LastName = reader["LastName"].ToString() ?? string.Empty,
                Email = reader["Email"].ToString() ?? string.Empty,
                Phone = reader["Phone"].ToString() ?? string.Empty,
                Salt = reader["Salt"].ToString() ?? string.Empty,
                Password = reader["Password"].ToString() ?? string.Empty,
                Username = reader["Username"].ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = reader["CreatedAt"] as DateTime?,
                UpdatedAt = reader["UpdatedAt"] as DateTime?
            };
        }
        user.Roles = await GetRolesAsync(user.Id);
        return user;
    }

    public async Task<User> UpdateUser(User user)
    {
        await using var connection = await _dbHelper.GetOpenConnectionAsync();
        await using var cmd = new MySqlCommand("UpdateUser", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@p_Id", user.Id);
        cmd.Parameters.AddWithValue("@p_FirstName", user.FirstName);
        cmd.Parameters.AddWithValue("@p_LastName", user.LastName);
        cmd.Parameters.AddWithValue("@p_Email", user.Email);
        cmd.Parameters.AddWithValue("@p_Username", user.Username);
        cmd.Parameters.AddWithValue("@p_Phone", user.Phone);
        
        await cmd.ExecuteNonQueryAsync();
        return user;
        
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        // Step 2: Establish database connection
        await using var connection = await _dbHelper.GetOpenConnectionAsync();

        // Step 3: Create and execute a stored procedure command
        await using var cmd = new MySqlCommand("CheckUsernameExists", connection);
        cmd.CommandType = System.Data.CommandType.StoredProcedure;

        // Add parameters to match the stored procedure inputs
        cmd.Parameters.Add(new MySqlParameter("@p_Username", MySqlDbType.VarChar, 50)).Value = username;

        // Add output parameter for result code
        cmd.Parameters.Add(new MySqlParameter("@p_exists", MySqlDbType.Bool) { Direction = ParameterDirection.Output });

        await cmd.ExecuteNonQueryAsync();

        var resultCode = (bool)cmd.Parameters["@p_exists"].Value;
        return resultCode;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        // Step 2: Establish database connection
        await using var connection = await _dbHelper.GetOpenConnectionAsync();

        // Step 3: Create and execute a stored procedure command
        await using var cmd = new MySqlCommand("CheckEmailExists", connection);
        cmd.CommandType = System.Data.CommandType.StoredProcedure;

        // Add parameters to match the stored procedure inputs
        cmd.Parameters.Add(new MySqlParameter("@p_Email", MySqlDbType.VarChar, 50)).Value = email;

        // Add output parameter for result code
        cmd.Parameters.Add(new MySqlParameter("@p_exists", MySqlDbType.Bool) { Direction = ParameterDirection.Output });

        await cmd.ExecuteNonQueryAsync();

        var resultCode = (bool)cmd.Parameters["@p_exists"].Value;
        return resultCode;
    }


    /// <summary>
    /// Retrieves all Roles via the GetAllRoles stored procedure.
    /// </summary>
    /// <returns>List of Role objects.</returns>
    public async Task<List<User>> GetUsers()
    {
        var users = new List<User>();

        await using var connection = await _dbHelper.GetOpenConnectionAsync();
        await using var cmd = new MySqlCommand("GetAllUsers", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var user = new User()
            {
                Id = Convert.ToInt32(reader["Id"]),
                FirstName = reader["FirstName"].ToString() ?? string.Empty,
                LastName = reader["LastName"].ToString() ?? string.Empty,
                Email = reader["Email"].ToString() ?? string.Empty,
                Phone = reader["Phone"].ToString() ?? string.Empty,
                Salt = reader["Salt"].ToString() ?? string.Empty,
                Password = reader["Password"].ToString() ?? string.Empty,
                Username = reader["Username"].ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = reader["CreatedAt"] as DateTime?,
                UpdatedAt = reader["UpdatedAt"] as DateTime?
            };
            users.Add(user);
        }
        return users;
    }
}