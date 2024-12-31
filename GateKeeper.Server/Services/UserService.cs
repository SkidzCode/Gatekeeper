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
    private readonly IDbHelper _dbHelper;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Constructor for the UserController.
    /// </summary>
    /// <param name="configuration">Application configuration dependency.</param>
    public UserService(IConfiguration configuration, IDbHelper dbHelper, ILogger<UserService> logger)
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
        await using var connection = await _dbHelper.GetWrapperAsync();

        // Step 3: Create and execute a stored procedure command
        var outputParameters = await connection.ExecuteNonQueryWithOutputAsync("AddUser", CommandType.StoredProcedure,
            new MySqlParameter("@p_FirstName", MySqlDbType.VarChar, 50) { Value = user.FirstName },
            new MySqlParameter("@p_LastName", MySqlDbType.VarChar, 50) { Value = user.LastName },
            new MySqlParameter("@p_Email", MySqlDbType.VarChar, 100) { Value = user.Email },
            new MySqlParameter("@p_Username", MySqlDbType.VarChar, 50) { Value = user.Username },
            new MySqlParameter("@p_Password", MySqlDbType.VarChar, 255) { Value = hashedPassword },
            new MySqlParameter("@p_Salt", MySqlDbType.VarChar, 255) { Value = salt },
            new MySqlParameter("@p_Phone", MySqlDbType.VarChar, 15) { Value = user.Phone },
            new MySqlParameter("@p_ResultCode", MySqlDbType.Int32) { Direction = ParameterDirection.Output },
            new MySqlParameter("last_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output });

        var resultCode = (int)outputParameters["@p_ResultCode"];
        if (outputParameters["last_id"] != DBNull.Value)
        {
            user.Id = Convert.ToInt32(outputParameters["last_id"]);
        }
        return (resultCode, user);
    }

    public async Task<int> ChangePassword(int userId, string newPassword)
    {
        await using var connection = await _dbHelper.GetWrapperAsync();

        var salt = PasswordHelper.GenerateSalt();
        var hashedPassword = PasswordHelper.HashPassword(newPassword, salt);

        await connection.ExecuteNonQueryAsync("PasswordChange", CommandType.StoredProcedure,
            new MySqlParameter("@p_HashedPassword", MySqlDbType.VarChar, 255) { Value = hashedPassword },
            new MySqlParameter("@p_Salt", MySqlDbType.VarChar, 255) { Value = salt },
            new MySqlParameter("@p_UserId", MySqlDbType.Int32) { Value = userId });

        return 1;
    }

    public async Task<List<string>> GetRolesAsync(int Id)
    {
        await using var connection = await _dbHelper.GetWrapperAsync();

        var roles = new List<string>();

        await using var reader = await connection.ExecuteReaderAsync("GetUserRoles", CommandType.StoredProcedure,
            new MySqlParameter("@p_UserId", MySqlDbType.Int32) { Value = Id });

        while (await reader.ReadAsync())
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
        await using var connection = await _dbHelper.GetWrapperAsync();
        User? user = null;

        var userReader = await connection.ExecuteReaderAsync("GetUserProfileByIdentifier", CommandType.StoredProcedure,
            new MySqlParameter("@p_Identifier", MySqlDbType.VarChar, 50) { Value = identifier });

        if (await userReader.ReadAsync())
        {
            user = new User()
            {
                Id = Convert.ToInt32(userReader["Id"]),
                FirstName = userReader["FirstName"].ToString() ?? string.Empty,
                LastName = userReader["LastName"].ToString() ?? string.Empty,
                Email = userReader["Email"].ToString() ?? string.Empty,
                Phone = userReader["Phone"].ToString() ?? string.Empty,
                Salt = userReader["Salt"].ToString() ?? string.Empty,
                Password = userReader["Password"].ToString() ?? string.Empty,
                Username = userReader["Username"].ToString() ?? string.Empty,
                Roles = new List<string>() // Initialize the Roles property
            };
            if (!await userReader.NextResultAsync()) return user;
            while (await userReader.ReadAsync())
            {
                var roleName = userReader["RoleName"]?.ToString();
                if (!string.IsNullOrEmpty(roleName))
                {
                    user?.Roles.Add(roleName);
                }
            }
        }
        return user;
    }

    public async Task<User?> GetUser(int identifier)
    {
        await using var connection = await _dbHelper.GetWrapperAsync();
        User? user = null;

        var userReader = await connection.ExecuteReaderAsync("GetUserProfile", CommandType.StoredProcedure,
            new MySqlParameter("@p_UserId", MySqlDbType.VarChar, 50) { Value = identifier });

        if (await userReader.ReadAsync())
        {
            user = new User()
            {
                Id = Convert.ToInt32(userReader["Id"]),
                FirstName = userReader["FirstName"].ToString() ?? string.Empty,
                LastName = userReader["LastName"].ToString() ?? string.Empty,
                Email = userReader["Email"].ToString() ?? string.Empty,
                Phone = userReader["Phone"].ToString() ?? string.Empty,
                Salt = userReader["Salt"].ToString() ?? string.Empty,
                Password = userReader["Password"].ToString() ?? string.Empty,
                Username = userReader["Username"].ToString() ?? string.Empty,
                Roles = new List<string>() // Initialize the Roles property
            };
            if (!await userReader.NextResultAsync()) return user;
            while (await userReader.ReadAsync())
            {
                var roleName = userReader["RoleName"]?.ToString();
                if (!string.IsNullOrEmpty(roleName))
                {
                    user?.Roles.Add(roleName);
                }
                else
                    break;
            }
        }
        return user;
    }

    public async Task<User> UpdateUser(User user)
    {
        await using var connection = await _dbHelper.GetWrapperAsync();

        await connection.ExecuteNonQueryAsync("UpdateUser", CommandType.StoredProcedure,
            new MySqlParameter("@p_Id", MySqlDbType.Int32) { Value = user.Id },
            new MySqlParameter("@p_FirstName", MySqlDbType.VarChar, 50) { Value = user.FirstName },
            new MySqlParameter("@p_LastName", MySqlDbType.VarChar, 50) { Value = user.LastName },
            new MySqlParameter("@p_Email", MySqlDbType.VarChar, 100) { Value = user.Email },
            new MySqlParameter("@p_Username", MySqlDbType.VarChar, 50) { Value = user.Username },
            new MySqlParameter("@p_Phone", MySqlDbType.VarChar, 15) { Value = user.Phone });

        return user;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        await using var connection = await _dbHelper.GetWrapperAsync();

        var outputParameters = await connection.ExecuteNonQueryWithOutputAsync("CheckUsernameExists", CommandType.StoredProcedure,
            new MySqlParameter("@p_Username", MySqlDbType.VarChar, 50) { Value = username },
            new MySqlParameter("@p_exists", MySqlDbType.Bool) { Direction = ParameterDirection.Output });

        var resultCode = (bool)outputParameters["@p_exists"];
        return resultCode;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        await using var connection = await _dbHelper.GetWrapperAsync();

        var outputParameters = await connection.ExecuteNonQueryWithOutputAsync("CheckEmailExists", CommandType.StoredProcedure,
            new MySqlParameter("@p_Email", MySqlDbType.VarChar, 50) { Value = email },
            new MySqlParameter("@p_exists", MySqlDbType.Bool) { Direction = ParameterDirection.Output });

        var resultCode = (bool)outputParameters["@p_exists"];
        return resultCode;
    }

    /// <summary>
    /// Retrieves all Roles via the GetAllRoles stored procedure.
    /// </summary>
    /// <returns>List of Role objects.</returns>
    public async Task<List<User>> GetUsers()
    {
        var users = new List<User>();

        await using var connection = await _dbHelper.GetWrapperAsync();

        await using var reader = await connection.ExecuteReaderAsync("GetAllUsers", CommandType.StoredProcedure);

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
                UpdatedAt = reader["UpdatedAt"] as DateTime?,
                Roles = new List<string>() // Initialize the Roles property
            };
            users.Add(user);
        }
        return users;
    }
}
