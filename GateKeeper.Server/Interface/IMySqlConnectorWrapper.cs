using MySqlConnector;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface IMySqlConnectorWrapper : IAsyncDisposable
    {
        Task<IMySqlConnectorWrapper> OpenConnectionAsync();
        Task<int> ExecuteNonQueryAsync(string commandText, CommandType commandType, params MySqlParameter[] parameters);
        Task<IMySqlDataReaderWrapper> ExecuteReaderAsync(string commandText, CommandType commandType, params MySqlParameter[] parameters);
        void CloseConnection();
        Task<Dictionary<string, object>> ExecuteNonQueryWithOutputAsync(string commandText, CommandType commandType, params MySqlParameter[] parameters);
    }
}
