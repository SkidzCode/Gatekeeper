using MySqlConnector;
using System.Data;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Services;

public interface IDBHelper
{
    Task<MySqlConnection> GetOpenConnectionAsync();
}

public class DBHelper: IDBHelper
{
    public readonly string ConnectionString;

    public DBHelper(IConfiguration configuration) 
    {
        var dbConfig = configuration.GetSection("DatabaseConfig").Get<DatabaseConfig>() ?? new DatabaseConfig();
        ConnectionString = $"Server={dbConfig.Server};Database={dbConfig.Database};Uid={dbConfig.User};Pwd={dbConfig.Password};Pooling=true;Maximum Pool Size=100;Connection Lifetime=300"; // Include pooling options here
    }

    public async Task<MySqlConnection> GetOpenConnectionAsync()
    {
        var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}



