using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account.UserModels;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _dbConnection;

        public UserRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<(int? userId, int resultCode)> AddUserAsync(User user, string salt)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_FirstName", user.FirstName, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@p_LastName", user.LastName, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@p_Email", user.Email, DbType.String, ParameterDirection.Input, 100);
            parameters.Add("@p_Username", user.Username, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@p_Password", user.Password, DbType.String, ParameterDirection.Input, 255); // Assuming pre-hashed
            parameters.Add("@p_Salt", salt, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_Phone", user.Phone, DbType.String, ParameterDirection.Input, 15);
            parameters.Add("@p_ResultCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@last_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync("AddUser", parameters, commandType: CommandType.StoredProcedure);

            int resultCode = parameters.Get<int>("@p_ResultCode");
            int? userId = null;
            try
            {
                // last_id might not be set if AddUser fails (e.g. user exists), Dapper might throw if it's not found.
                // The SP sets last_id to NULL in case of failure, so Get<int?> should handle this.
                userId = parameters.Get<int?>("@last_id");
            }
            catch (System.Exception ex) // Catch if parameter isn't found, though Get<int?> should be safer
            {
                 // Log this occurrence if necessary, for now, userId remains null
                 System.Console.WriteLine($"Error retrieving @last_id: {ex.Message}");
            }


            return (userId, resultCode);
        }

        public async Task<bool> ChangePasswordAsync(int userId, string hashedPassword, string salt)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", userId, DbType.Int32);
            parameters.Add("@p_HashedPassword", hashedPassword, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_Salt", salt, DbType.String, ParameterDirection.Input, 255);

            var rowsAffected = await _dbConnection.ExecuteAsync("PasswordChange", parameters, commandType: CommandType.StoredProcedure);
            return rowsAffected > 0;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Email", email, DbType.String, ParameterDirection.Input, 100); // Changed size to 100 to match AddUser
            parameters.Add("@p_exists", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync("CheckEmailExists", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<bool>("@p_exists");
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            // Assuming GetAllUsers returns all necessary fields that map to the User model.
            // If roles need to be fetched per user, this would become more complex,
            // potentially N+1 queries or a more complex single query.
            // For now, consistent with previous UserService.GetUsers, roles are not fetched here.
            return await _dbConnection.QueryAsync<User>("GetAllUsers", commandType: CommandType.StoredProcedure);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", id, DbType.Int32);

            using (var multi = await _dbConnection.QueryMultipleAsync("GetUserProfile", parameters, commandType: CommandType.StoredProcedure))
            {
                var user = await multi.ReadFirstOrDefaultAsync<User>();
                if (user != null)
                {
                    // The SP GetUserProfile returns roles as a second result set
                    user.Roles = (await multi.ReadAsync<string>()).ToList();
                }
                return user;
            }
        }

        public async Task<User?> GetUserByIdentifierAsync(string identifier)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Identifier", identifier, DbType.String, ParameterDirection.Input, 100); // Max length of email or username

            // Assuming GetUserProfileByIdentifier SP is created or GetUser is adapted
            // and it returns user details then roles, similar to GetUserProfile
            using (var multi = await _dbConnection.QueryMultipleAsync("GetUserProfileByIdentifier", parameters, commandType: CommandType.StoredProcedure))
            {
                var user = await multi.ReadFirstOrDefaultAsync<User>();
                if (user != null)
                {
                    user.Roles = (await multi.ReadAsync<string>()).ToList();
                }
                return user;
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", userId, DbType.Int32);
            return await _dbConnection.QueryAsync<string>("GetUserRoles", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", user.Id, DbType.Int32);
            parameters.Add("@p_FirstName", user.FirstName, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@p_LastName", user.LastName, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@p_Email", user.Email, DbType.String, ParameterDirection.Input, 100);
            parameters.Add("@p_Username", user.Username, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@p_Phone", user.Phone, DbType.String, ParameterDirection.Input, 15);

            // The stored procedure "UpdateUserPic" is used in existing UserService for user updates including picture.
            // If user.ProfilePicture is null, DBNull.Value should be passed.
            parameters.Add("@p_ProfilePicture", user.ProfilePicture, DbType.Binary, ParameterDirection.Input);


            var rowsAffected = await _dbConnection.ExecuteAsync("UpdateUserPic", parameters, commandType: CommandType.StoredProcedure);
            return rowsAffected > 0;
        }

        public async Task UpdateUserRolesAsync(int userId, IEnumerable<string> roleNames)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@pUserId", userId, DbType.Int32);
            parameters.Add("@pRoleNames", string.Join(",", roleNames), DbType.String, ParameterDirection.Input, 1000); // Max length based on SP

            await _dbConnection.ExecuteAsync("UserRolesUpdate", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Username", username, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@p_exists", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync("CheckUsernameExists", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<bool>("@p_exists");
        }
    }
}
