using System;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface IKeyManagementRepository
    {
        Task InsertNewKeyAsync(byte[] encryptedKey, DateTime expirationDate);
        Task<byte[]?> GetActiveEncryptedKeyAsync();
        Task DeactivateOldKeysAsync(); // Assuming this might be needed later
    }
}
