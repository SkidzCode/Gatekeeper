using MySqlConnector;
using System.Data;
using GateKeeper.Server.Database;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Interface;

namespace GateKeeper.Server.Services;

public class DBHelper : IDbHelper
{
    public readonly string ConnectionString;

    public DBHelper(IConfiguration configuration)
    {
        var dbConfig = configuration.GetSection("DatabaseConfig").Get<DatabaseConfig>() ?? new DatabaseConfig();
        ConnectionString = $"Server={dbConfig.Server};Database={dbConfig.Database};Uid={dbConfig.User};Pwd={dbConfig.Password};Pooling=true;Maximum Pool Size=100;Connection Lifetime=300"; // Include pooling options here
    }

    public async Task<IMySqlConnectorWrapper> GetWrapperAsync()
    {
        return await new MySqlConnectorWrapper(ConnectionString).OpenConnectionAsync();
    }
}



