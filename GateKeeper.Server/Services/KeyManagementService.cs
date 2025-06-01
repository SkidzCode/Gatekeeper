using System;
using System.Data;
using System.Security;
using System.Security.Cryptography;
using GateKeeper.Server.Interface;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using MySqlConnector;


using Microsoft.Extensions.Options; // Added for IOptions
using GateKeeper.Server.Models.Configuration; // Added for KeyManagementConfig

namespace GateKeeper.Server.Services
{
    public class KeyManagementService : IKeyManagementService
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<KeyManagementService> _logger;
        private readonly byte[] _masterEncryptionKeyBytes; // Changed from _masterEncryptionKey

        public KeyManagementService(
            IDbHelper dbHelper,
            ILogger<KeyManagementService> logger,
            IOptions<KeyManagementConfig> keyManagementConfigOptions) // Changed parameter
        {
            _dbHelper = dbHelper;
            _logger = logger;
            var keyManagementConfig = keyManagementConfigOptions.Value;

            if (string.IsNullOrEmpty(keyManagementConfig.MasterKey))
            {
                _logger.LogError("MasterKey is not configured in KeyManagementConfig.");
                throw new InvalidOperationException("MasterKey is not configured.");
            }
            try
            {
                _masterEncryptionKeyBytes = Convert.FromBase64String(keyManagementConfig.MasterKey);
                if (_masterEncryptionKeyBytes.Length != 32) // AES-256 requires a 32-byte key
                {
                    _logger.LogError("MasterKey, after Base64 decoding, is not 32 bytes long.");
                    throw new InvalidOperationException("MasterKey must be a 32-byte key after Base64 decoding.");
                }
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "MasterKey is not a valid Base64 string.");
                throw new InvalidOperationException("MasterKey is not a valid Base64 string.", ex);
            }
        }

        /// <summary>
        /// Creates a new random key for JWT signing, encrypts it,
        /// inserts it into DB, and marks it active. Also sets older keys to inactive if desired.
        /// </summary>
        public async Task RotateKeyAsync(DateTime expirationDate)
        {
            // 1) Generate random 32-byte key for HMAC-SHA256 or whichever algorithm you use.
            byte[] newKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(newKey);
            }

            // 2) Encrypt the new key using your masterEncryptionKey
            byte[] encryptedKey = EncryptKey(newKey);

            // 3) Insert the new encrypted key into the database
            //    Possibly deactivate older keys here or in a separate step.
            await InsertNewKeyAsync(encryptedKey, expirationDate);

            // 4) Optionally mark old active keys as inactive if you only allow 1 active at a time:
            //    await DeactivateOldKeysAsync();
        }

        /// <summary>
        /// Retrieves the currently active key from the DB, decrypts it, and returns it as a SecureString.
        /// </summary>
        public async Task<SecureString> GetCurrentKeyAsync()
        {
            byte[] encryptedKey = await GetActiveEncryptedKeyAsync();
            if (encryptedKey == null || encryptedKey.Length == 0)
            {
                _logger.LogWarning("No active key found in the database. Attempting to rotate key.");
                await RotateKeyAsync(DateTime.UtcNow.AddHours(24));
                encryptedKey = await GetActiveEncryptedKeyAsync();

                if (encryptedKey == null || encryptedKey.Length == 0)
                {
                    _logger.LogError("Still no active key found after attempting rotation. Cannot provide current key.");
                    return null;
                }
                _logger.LogInformation("Successfully rotated key and fetched the new active key.");
            }

            // Decrypt
            byte[] plainKey = DecryptKey(encryptedKey);

            // Convert to SecureString
            var secureString = new SecureString();
            foreach (char c in Convert.ToBase64String(plainKey))
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();

            // Zero out the plainKey from memory as soon as possible
            Array.Clear(plainKey, 0, plainKey.Length);

            return secureString;
        }

        private async Task InsertNewKeyAsync(byte[] encryptedKey, DateTime expirationDate)
        {
            const string storedProc = "spInsertKey";
            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("p_SecretKey", MySqlDbType.VarBinary) { Value = encryptedKey },
                new MySqlParameter("p_ExpirationDate", MySqlDbType.DateTime) { Value = expirationDate },
            };

            await using var wrapper = await _dbHelper.GetWrapperAsync();
            await wrapper.OpenConnectionAsync();
            await wrapper.ExecuteNonQueryAsync(storedProc, CommandType.StoredProcedure, parameters.ToArray());
        }

        private async Task<byte[]> GetActiveEncryptedKeyAsync()
        {
            const string storedProc = "spGetActiveKey";
            await using var wrapper = await _dbHelper.GetWrapperAsync();
            await wrapper.OpenConnectionAsync();

            await using var reader = await wrapper.ExecuteReaderAsync(storedProc, CommandType.StoredProcedure);
            if (await reader.ReadAsync())
            {
                return reader["SecretKey"] as byte[];
            }
            return null;
        }

        private byte[] EncryptKey(byte[] plainKey)
        {
            using var aes = Aes.Create();
            try
            {
                aes.Key = _masterEncryptionKeyBytes; // Use the byte array
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;

                using var encryptor = aes.CreateEncryptor();
                byte[] cipherText = encryptor.TransformFinalBlock(plainKey, 0, plainKey.Length);

                // Prepend IV to cipherText so we can use it when decrypting
                byte[] result = new byte[aes.IV.Length + cipherText.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                Buffer.BlockCopy(cipherText, 0, result, aes.IV.Length, cipherText.Length);

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private byte[] DecryptKey(byte[] cipherData)
        {
            using var aes = Aes.Create();
            aes.Key = _masterEncryptionKeyBytes; // Use the byte array
            aes.Mode = CipherMode.CBC;

            // Extract IV
            byte[] iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(cipherData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            int cipherTextLength = cipherData.Length - iv.Length;
            byte[] cipherText = new byte[cipherTextLength];
            Buffer.BlockCopy(cipherData, iv.Length, cipherText, 0, cipherTextLength);

            using var decryptor = aes.CreateDecryptor();
            byte[] plainKey = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

            return plainKey;
        }

        /// <summary>
        /// Optionally, if you want only 1 active key at a time, call this after inserting new key.
        /// </summary>
        private async Task DeactivateOldKeysAsync()
        {
            // You can first fetch the newly inserted Id and then
            // call 'spDeactivateKey' for all others, or run a custom SP to do it in one go.
        }
    }
}
