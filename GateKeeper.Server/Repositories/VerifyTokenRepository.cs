using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using MySqlConnector; // Required for MySqlDbType if used explicitly with Dapper parameters
using System;
using System.Data;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class VerifyTokenRepository : IVerifyTokenRepository
    {
        private readonly IDbConnection _dbConnection;

        public VerifyTokenRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<VerificationTokenDetails?> GetTokenDetailsForVerificationAsync(string tokenId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", tokenId, DbType.String, ParameterDirection.Input, 36); // Assuming GUID length

            // The "ValidateUser" SP returns multiple fields which should map to VerificationTokenDetails
            // Dapper will map them by name automatically if the column names in SP result match property names.
            return await _dbConnection.QueryFirstOrDefaultAsync<VerificationTokenDetails>(
                "ValidateUser",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<string> StoreTokenAsync(int userId, string verifyType, string hashedToken, string salt, DateTime expiryDate)
        {
            var tokenId = Guid.NewGuid().ToString();
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", tokenId, DbType.String, ParameterDirection.Input, 36);
            parameters.Add("@p_VerifyType", verifyType, DbType.String, ParameterDirection.Input, 20);
            parameters.Add("@p_UserId", userId, DbType.Int32);
            parameters.Add("@p_HashedToken", hashedToken, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_Salt", salt, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_ExpiryDate", expiryDate, DbType.DateTime);

            await _dbConnection.ExecuteAsync(
                "VerificationInsert",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            // The SP doesn't return the ID, we generate it beforehand and pass it in.
            return tokenId;
        }

        public async Task<int> RevokeTokensAsync(int userId, string verifyType, string? tokenId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", userId, DbType.Int32);
            parameters.Add("@p_TokenId", tokenId, DbType.String, ParameterDirection.Input, 36);
            parameters.Add("@p_VerifyType", verifyType, DbType.String, ParameterDirection.Input, 20);
            parameters.Add("@p_RowsAffected", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync(
                "RevokeVerifyToken",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return parameters.Get<int>("@p_RowsAffected");
        }

        public async Task<int> CompleteTokensAsync(int userId, string verifyType, string? tokenId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", userId, DbType.Int32);
            parameters.Add("@p_TokenId", tokenId, DbType.String, ParameterDirection.Input, 36);
            parameters.Add("@p_VerifyType", verifyType, DbType.String, ParameterDirection.Input, 20);
            parameters.Add("@p_RowsAffected", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync(
                "CompleteVerifyToken",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return parameters.Get<int>("@p_RowsAffected");
        }
    }
}
