using Dapper;
using GateKeeper.Server.Interface;
using MySqlConnector;
using System.Data;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    /// <summary>
    /// Repository handling database operations related to user authentication using Dapper.
    /// </summary>
    public class UserAuthenticationRepository : IUserAuthenticationRepository
    {
        private readonly IDbConnection _dbConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAuthenticationRepository"/> class.
        /// </summary>
        /// <param name="dbConnection">The database connection.</param>
        public UserAuthenticationRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        /// <inheritdoc />
        public async Task AssignRoleToUserAsync(int userId, string roleName)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", userId, DbType.Int32);
            parameters.Add("@p_RoleName", roleName, DbType.String, ParameterDirection.Input, 50); // Assuming RoleName max length 50

            await _dbConnection.ExecuteAsync("AssignRoleToUser", parameters, commandType: CommandType.StoredProcedure);
        }

        /// <inheritdoc />
        public async Task ValidateFinishAsync(int userId, string sessionId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", userId, DbType.Int32);
            // Assuming sessionId corresponds to @p_Id in the ValidateFinish stored procedure
            // The SP might have a different parameter name for sessionId, adjust if necessary.
            // Based on UserAuthenticationService, it seems to be @p_Id for the verification session.
            parameters.Add("@p_Id", sessionId, DbType.String); // Adjust DbType and size if known and different

            await _dbConnection.ExecuteAsync("ValidateFinish", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
