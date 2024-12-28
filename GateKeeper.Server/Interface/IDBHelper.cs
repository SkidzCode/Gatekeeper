using GateKeeper.Server.Services;

namespace GateKeeper.Server.Interface;

public interface IDBHelper
{
    Task<IMySqlConnectorWrapper> GetWrapperAsync();
}