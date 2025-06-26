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

        public async Task<byte[]> GetActiveEncryptedKeyAsync()
        {
            return await _dbConnection.QueryFirstOrDefaultAsync<byte[]>("spGetActiveKey", commandType: CommandType.StoredProcedure);
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
