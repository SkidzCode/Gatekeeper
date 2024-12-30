using System.Security;

namespace GateKeeper.Server.Interface;

public interface IKeyManagementService
{
    Task RotateKeyAsync(DateTime expirationDate);
    Task<SecureString> GetCurrentKeyAsync();
}
