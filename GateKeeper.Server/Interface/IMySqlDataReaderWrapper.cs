using System;
using System.Threading;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface IMySqlDataReaderWrapper : IAsyncDisposable
    {
        Task<bool> ReadAsync(CancellationToken cancellationToken = default);
        object this[string name] { get; }
        int GetInt32(string name);
        string GetString(string name);
        DateTime GetDateTime(string name);
        bool IsDBNull(int ordinal);
        int GetOrdinal(string name);
        Task<bool> NextResultAsync(CancellationToken cancellationToken = default);
    }
}
