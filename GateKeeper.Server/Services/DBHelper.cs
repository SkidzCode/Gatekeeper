using MySqlConnector;
using System.Data;
using GateKeeper.Server.Database;
using GateKeeper.Server.Models.Configuration; // Updated to use the new DatabaseConfig location
using GateKeeper.Server.Interface;
using Microsoft.Extensions.Options; // Added for IOptions

namespace GateKeeper.Server.Services;

public class DBHelper : IDbHelper
{
    private readonly DatabaseConfig _dbConfig;

    public DBHelper(IOptions<DatabaseConfig> dbConfigOptions)
    {
        _dbConfig = dbConfigOptions.Value;
        // Basic validation, though Program.cs handles more comprehensive validation
        if (string.IsNullOrWhiteSpace(_dbConfig.GateKeeperConnection))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }
    }

    public async Task<IMySqlConnectorWrapper> GetWrapperAsync()
    {
        // Using the ConnectionString directly from the injected and validated DatabaseConfig
        return await new MySqlConnectorWrapper(_dbConfig.GateKeeperConnection).OpenConnectionAsync();
    }
}



