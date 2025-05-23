using GateKeeper.Server.Interface;
using MySqlConnector;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GateKeeper.Server.Database
{
    public class MySqlDataReaderWrapper : IMySqlDataReaderWrapper, IAsyncDisposable
    {
        private readonly MySqlDataReader _reader;

        public MySqlDataReaderWrapper(MySqlDataReader reader)
        {
            _reader = reader;
        }

        public async Task<bool> ReadAsync(CancellationToken cancellationToken = default)
        {
            return await _reader.ReadAsync(cancellationToken);
        }

        public object this[string name] => _reader[name];

        public int GetInt32(string name)
        {
            return _reader.GetInt32(name);
        }

        public string GetString(string name)
        {
            return _reader.GetString(name);
        }

        public DateTime GetDateTime(string name)
        {
            return _reader.GetDateTime(name);
        }

        public bool IsDBNull(int ordinal)
        {
            return _reader.IsDBNull(ordinal);
        }

        public int GetOrdinal(string name)
        {
            return _reader.GetOrdinal(name);
        }

        public async Task<bool> NextResultAsync(CancellationToken cancellationToken = default)
        {
            return await _reader.NextResultAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _reader.DisposeAsync();
        }
    }
}
