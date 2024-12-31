using GateKeeper.Server.Services;

namespace GateKeeper.Server.Interface;

public interface IDbHelper
{
    Task<IMySqlConnectorWrapper> GetWrapperAsync();
}