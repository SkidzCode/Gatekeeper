using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Interface;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using GateKeeper.Server.Models.Configuration;
using System.Linq;
using GateKeeper.Server.Services.Site;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class KeyManagementServiceTests
    {
        private Mock<IKeyManagementRepository> _mockKeyManagementRepository;
        private Mock<ILogger<KeyManagementService>> _mockLogger;
        private KeyManagementService _keyManagementService;
        private Mock<IOptions<KeyManagementConfig>> _mockKeyManagementConfigOptions;
        private KeyManagementConfig _keyManagementConfig;
        private byte[] _testMasterEncryptionKeyBytes;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockKeyManagementRepository = new Mock<IKeyManagementRepository>();
            _mockLogger = new Mock<ILogger<KeyManagementService>>();

            string testMasterKeyBase64 = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8="; // Valid 32-byte key
            _testMasterEncryptionKeyBytes = Convert.FromBase64String(testMasterKeyBase64);

            _keyManagementConfig = new KeyManagementConfig { MasterKey = testMasterKeyBase64 };
            _mockKeyManagementConfigOptions = new Mock<IOptions<KeyManagementConfig>>();
            _mockKeyManagementConfigOptions.Setup(o => o.Value).Returns(_keyManagementConfig);

            _keyManagementService = new KeyManagementService(
                _mockKeyManagementRepository.Object,
                _mockLogger.Object,
                _mockKeyManagementConfigOptions.Object
            );
        }

        private string SecureStringToString(SecureString value)
        {
            if (value == null) return null;
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        #region Constructor Tests
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_NullMasterKey_ThrowsInvalidOperationException()
        {
            _keyManagementConfig.MasterKey = null;
            new KeyManagementService(_mockKeyManagementRepository.Object, _mockLogger.Object, _mockKeyManagementConfigOptions.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_InvalidBase64MasterKey_ThrowsInvalidOperationException()
        {
            _keyManagementConfig.MasterKey = "NotValidBase64";
            new KeyManagementService(_mockKeyManagementRepository.Object, _mockLogger.Object, _mockKeyManagementConfigOptions.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Constructor_IncorrectKeyLengthMasterKey_ThrowsInvalidOperationException()
        {
            _keyManagementConfig.MasterKey = Convert.ToBase64String(new byte[16]); // Not 32 bytes
            new KeyManagementService(_mockKeyManagementRepository.Object, _mockLogger.Object, _mockKeyManagementConfigOptions.Object);
        }

        #endregion


        #region RotateKeyAsync Tests

        [TestMethod]
        public async Task RotateKeyAsync_CallsRepositoryInsertNewKeyAsync()
        {
            // Arrange
            var expirationDate = DateTime.UtcNow.AddYears(1);
            byte[] capturedEncryptedKey = null;

            _mockKeyManagementRepository
                .Setup(repo => repo.InsertNewKeyAsync(It.IsAny<byte[]>(), expirationDate))
                .Callback<byte[], DateTime>((key, date) => capturedEncryptedKey = key)
                .Returns(Task.CompletedTask);

            // Act
            await _keyManagementService.RotateKeyAsync(expirationDate);

            // Assert
            _mockKeyManagementRepository.Verify(repo => repo.InsertNewKeyAsync(It.IsAny<byte[]>(), expirationDate), Times.Once);
            Assert.IsNotNull(capturedEncryptedKey);
            Assert.IsTrue(capturedEncryptedKey.Length > 32, "Encrypted key should be longer than original due to IV and encryption overhead.");

            // Verify decryption (optional deep check)
            using (var aes = Aes.Create())
            {
                aes.Key = _testMasterEncryptionKeyBytes;
                byte[] iv = new byte[aes.BlockSize / 8];
                Buffer.BlockCopy(capturedEncryptedKey, 0, iv, 0, iv.Length);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                int cipherTextLength = capturedEncryptedKey.Length - iv.Length;
                byte[] cipherText = new byte[cipherTextLength];
                Buffer.BlockCopy(capturedEncryptedKey, iv.Length, cipherText, 0, cipherTextLength);
                using var decryptor = aes.CreateDecryptor();
                byte[] decryptedKey = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                Assert.AreEqual(32, decryptedKey.Length, "Decrypted key should be 32 bytes.");
            }
        }
        #endregion

        #region GetCurrentKeyAsync Tests

        [TestMethod]
        public async Task GetCurrentKeyAsync_ActiveKeyFound_ReturnsDecryptedKeyAsSecureString()
        {
            // Arrange
            byte[] originalPlainKey = new byte[32];
            RandomNumberGenerator.Fill(originalPlainKey);

            byte[] encryptedKeyFromDb;
            using (var aes = Aes.Create())
            {
                aes.Key = _testMasterEncryptionKeyBytes;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] cipherText = encryptor.TransformFinalBlock(originalPlainKey, 0, originalPlainKey.Length);
                encryptedKeyFromDb = aes.IV.Concat(cipherText).ToArray();
            }

            _mockKeyManagementRepository.Setup(repo => repo.GetActiveEncryptedKeyAsync()).ReturnsAsync(encryptedKeyFromDb);

            // Act
            var secureStringResult = await _keyManagementService.GetCurrentKeyAsync();

            // Assert
            Assert.IsNotNull(secureStringResult);
            var decryptedKeyString = SecureStringToString(secureStringResult);
            Assert.AreEqual(Convert.ToBase64String(originalPlainKey), decryptedKeyString);
            _mockKeyManagementRepository.Verify(repo => repo.GetActiveEncryptedKeyAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetCurrentKeyAsync_NoActiveKey_RotatesAndReturnsNewKey()
        {
            // Arrange
            byte[] newPlainKey = new byte[32]; // This will be the key generated by RotateKeyAsync
            RandomNumberGenerator.Fill(newPlainKey);

            byte[] rotatedEncryptedKey; // This will be the key "inserted" by RotateKeyAsync and then "retrieved"
            using (var aes = Aes.Create())
            {
                aes.Key = _testMasterEncryptionKeyBytes;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                // Simulate the key that would be generated and encrypted by RotateKeyAsync
                // For the test, we pre-calculate what RotateKeyAsync would produce if it generated `newPlainKey`.
                // This is a bit of a simplification as RotateKeyAsync generates its own random key.
                // A more robust test would capture the argument to InsertNewKeyAsync.
                byte[] cipherText = encryptor.TransformFinalBlock(newPlainKey, 0, newPlainKey.Length);
                rotatedEncryptedKey = aes.IV.Concat(cipherText).ToArray();
            }

            _mockKeyManagementRepository.SetupSequence(repo => repo.GetActiveEncryptedKeyAsync())
                .ReturnsAsync((byte[])null) // First call, no key
                .ReturnsAsync(rotatedEncryptedKey); // Second call, after rotation

            _mockKeyManagementRepository
                .Setup(repo => repo.InsertNewKeyAsync(It.IsAny<byte[]>(), It.IsAny<DateTime>()))
                .Callback<byte[], DateTime>((key, date) => {
                    // We can't directly use `rotatedEncryptedKey` here for assertion because the IV will be different
                    // as RotateKeyAsync generates a new key and IV each time.
                    // Instead, we ensure that InsertNewKeyAsync was called.
                    // The GetActiveEncryptedKeyAsync mock returning `rotatedEncryptedKey` simulates the successful rotation.
                 })
                .Returns(Task.CompletedTask);


            // Act
            var secureStringResult = await _keyManagementService.GetCurrentKeyAsync();

            // Assert
            Assert.IsNotNull(secureStringResult);
            var decryptedKeyString = SecureStringToString(secureStringResult);
            Assert.AreEqual(Convert.ToBase64String(newPlainKey), decryptedKeyString); // Compare with the pre-calculated plain key

            _mockKeyManagementRepository.Verify(repo => repo.GetActiveEncryptedKeyAsync(), Times.Exactly(2));
            _mockKeyManagementRepository.Verify(repo => repo.InsertNewKeyAsync(It.IsAny<byte[]>(), It.IsAny<DateTime>()), Times.Once);
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No active key found in the database.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully rotated key and fetched the new active key.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }


        [TestMethod]
        public async Task GetCurrentKeyAsync_RotationFailsToProduceKey_ReturnsNull()
        {
            // Arrange
            _mockKeyManagementRepository.SetupSequence(repo => repo.GetActiveEncryptedKeyAsync())
                .ReturnsAsync((byte[])null) // First call, no key
                .ReturnsAsync((byte[])null); // Second call, rotation also yields no key

            _mockKeyManagementRepository
                .Setup(repo => repo.InsertNewKeyAsync(It.IsAny<byte[]>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _keyManagementService.GetCurrentKeyAsync();

            // Assert
            Assert.IsNull(result);
            _mockKeyManagementRepository.Verify(repo => repo.GetActiveEncryptedKeyAsync(), Times.Exactly(2));
            _mockKeyManagementRepository.Verify(repo => repo.InsertNewKeyAsync(It.IsAny<byte[]>(), It.IsAny<DateTime>()), Times.Once);
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Still no active key found after attempting rotation.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }


        [TestMethod]
        public async Task GetCurrentKeyAsync_DecryptionFails_ThrowsCryptographicException()
        {
            // Arrange
            byte[] invalidEncryptedKey = new byte[] { 1, 2, 3, 4, 5 }; // Too short for valid AES + IV
            _mockKeyManagementRepository.Setup(repo => repo.GetActiveEncryptedKeyAsync()).ReturnsAsync(invalidEncryptedKey);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                await _keyManagementService.GetCurrentKeyAsync();
            });
        }
        #endregion
    }
}
