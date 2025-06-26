using Dapper;
using GateKeeper.Server.Interface;
using System;
using System.Data;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class KeyManagementRepository : IKeyManagementRepository
    {
        private readonly IDbConnection _dbConnection;

        public KeyManagementRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task InsertNewKeyAsync(byte[] encryptedKey, DateTime expirationDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_SecretKey", encryptedKey, DbType.Binary);
            parameters.Add("p_ExpirationDate", expirationDate, DbType.DateTime);

            await _dbConnection.ExecuteAsync("spInsertKey", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<byte[]?> GetActiveEncryptedKeyAsync()
        {
            // Call the SP, then select the SecretKey property from the result.
            // This assumes the SP returns a single row or null.
            var result = await _dbConnection.QuerySingleOrDefaultAsync(
                "spGetActiveKey",
                commandType: CommandType.StoredProcedure
            );

            if (result == null)
            {
                return null;
            }

            // Dapper returns a DapperRow (which can be treated as IDictionary<string, object>)
            // or a specific type if you map it. Since we used no type, it's dynamic.
            return result.SecretKey as byte[];
        }

        public async Task DeactivateOldKeysAsync()
        {
            // Implementation for deactivating old keys, if needed.
            // For example, calling a stored procedure like "spDeactivateOldKeys"
            // await _dbConnection.ExecuteAsync("spDeactivateOldKeys", commandType: CommandType.StoredProcedure);
            await Task.CompletedTask; // Placeholder
        }
    }
}
