using GateKeeper.Server.Models.Account;
using MySqlConnector;
using System.Data;
using GateKeeper.Server.Interface;

namespace GateKeeper.Server.Database
{
    // Interface defining the wrapper's capabilities


    // Concrete class implementing the interface
    public class MySqlConnectorWrapper : IMySqlConnectorWrapper
    {
        private readonly MySqlConnection _connection;

        public MySqlConnectorWrapper(string connectionString) => 
            _connection = new MySqlConnection(connectionString);

        public async Task<IMySqlConnectorWrapper> OpenConnectionAsync()
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            return this;
        }

        public async Task<int> ExecuteNonQueryAsync(string commandText, CommandType commandType, params MySqlParameter[] parameters)
        {
            await using var command = CreateCommand(commandText, commandType, parameters);
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<IMySqlDataReaderWrapper> ExecuteReaderAsync(string commandText, CommandType commandType, params MySqlParameter[] parameters)
        {
            await using var command = CreateCommand(commandText, commandType, parameters);
            var reader = await command.ExecuteReaderAsync();
            return new MySqlDataReaderWrapper(reader);
        }

        private MySqlCommand CreateCommand(string commandText, CommandType commandType, params MySqlParameter[] parameters)
        {
            var command = new MySqlCommand(commandText, _connection)
            {
                CommandType = commandType
            };
            command.Parameters.AddRange(parameters);
            return command;
        }

        public void CloseConnection()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        public async Task<Dictionary<string, object>> ExecuteNonQueryWithOutputAsync(string commandText, CommandType commandType, params MySqlParameter[] parameters)
        {
            await using var command = CreateCommand(commandText, commandType, parameters);
            await command.ExecuteNonQueryAsync();

            var outputValues = new Dictionary<string, object>();
            foreach (MySqlParameter parameter in command.Parameters)
            {
                if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.InputOutput)
                {
                    outputValues[parameter.ParameterName] = parameter.Value;
                }
            }

            return outputValues;
        }

        public ValueTask DisposeAsync()
        {
            CloseConnection();
            return _connection.DisposeAsync();
        }

    }
}