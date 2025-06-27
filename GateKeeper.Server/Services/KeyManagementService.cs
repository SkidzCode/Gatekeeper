using System;
using System.Security;
using System.Security.Cryptography;
using GateKeeper.Server.Interface;
using Microsoft.Extensions.Options;
using GateKeeper.Server.Models.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GateKeeper.Server.Services
{
    public class KeyManagementService : IKeyManagementService
    {
        private readonly IKeyManagementRepository _keyManagementRepository;
        private readonly ILogger<KeyManagementService> _logger;
        private readonly byte[] _masterEncryptionKeyBytes;

        public KeyManagementService(
            IKeyManagementRepository keyManagementRepository,
            ILogger<KeyManagementService> logger,
            IOptions<KeyManagementConfig> keyManagementConfigOptions)
        {
            _keyManagementRepository = keyManagementRepository;
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

        public async Task RotateKeyAsync(DateTime expirationDate)
        {
            byte[] newKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(newKey);
            }

            byte[] encryptedKey = EncryptKey(newKey);
            await _keyManagementRepository.InsertNewKeyAsync(encryptedKey, expirationDate);
            // Optionally: await _keyManagementRepository.DeactivateOldKeysAsync();
        }

        public async Task<SecureString> GetCurrentKeyAsync()
        {
            byte[] encryptedKey = await _keyManagementRepository.GetActiveEncryptedKeyAsync();
            if (encryptedKey == null || encryptedKey.Length == 0)
            {
                _logger.LogWarning("No active key found in the database. Attempting to rotate key.");
                await RotateKeyAsync(DateTime.UtcNow.AddHours(24)); // Consider making rotation duration configurable
                encryptedKey = await _keyManagementRepository.GetActiveEncryptedKeyAsync();

                if (encryptedKey == null || encryptedKey.Length == 0)
                {
                    _logger.LogError("Still no active key found after attempting rotation. Cannot provide current key.");
                    return null;
                }
                _logger.LogInformation("Successfully rotated key and fetched the new active key.");
            }

            byte[] plainKey = DecryptKey(encryptedKey);

            var secureString = new SecureString();
            foreach (char c in Convert.ToBase64String(plainKey)) // Storing Base64 of the key
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();

            Array.Clear(plainKey, 0, plainKey.Length);

            return secureString;
        }

        private byte[] EncryptKey(byte[] plainKey)
        {
            using var aes = Aes.Create();
            aes.Key = _masterEncryptionKeyBytes;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC; // Ensure this matches your decryption mode

            using var encryptor = aes.CreateEncryptor();
            byte[] cipherText = encryptor.TransformFinalBlock(plainKey, 0, plainKey.Length);

            byte[] result = new byte[aes.IV.Length + cipherText.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherText, 0, result, aes.IV.Length, cipherText.Length);

            return result;
        }

        private byte[] DecryptKey(byte[] cipherData)
        {
            using var aes = Aes.Create();
            aes.Key = _masterEncryptionKeyBytes;
            aes.Mode = CipherMode.CBC; // Ensure this matches your encryption mode

            byte[] iv = new byte[aes.BlockSize / 8];
            if (cipherData.Length < iv.Length)
            {
                _logger.LogError("Cipher data is too short to contain an IV.");
                throw new ArgumentException("Cipher data is too short to contain an IV.", nameof(cipherData));
            }
            Buffer.BlockCopy(cipherData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            int cipherTextLength = cipherData.Length - iv.Length;
            if (cipherTextLength < 0) // Should be caught by previous check, but good for safety
            {
                _logger.LogError("Cipher data length implies negative ciphertext length after IV extraction.");
                throw new ArgumentException("Invalid cipher data format.", nameof(cipherData));
            }
            byte[] cipherText = new byte[cipherTextLength];
            Buffer.BlockCopy(cipherData, iv.Length, cipherText, 0, cipherTextLength);

            using var decryptor = aes.CreateDecryptor();
            try
            {
                byte[] plainKey = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                return plainKey;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Decryption failed. This could be due to an incorrect master key, corrupted data, or padding issues.");
                // Depending on policy, you might re-throw, or return null/empty to indicate failure.
                // Re-throwing is generally better to signal a critical failure.
                throw;
            }
        }
    }
}
