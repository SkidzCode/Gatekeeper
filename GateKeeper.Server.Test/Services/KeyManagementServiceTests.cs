using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices; // For Marshal
using System.Security.Cryptography; // For CryptographicException
using MySqlConnector; // For MySqlParameter
using System.Data; // For CommandType
using System.Linq; // For Enumerable.SequenceEqual

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class KeyManagementServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<IMySqlConnectorWrapper> _mockMySqlConnectorWrapper;
        private Mock<ILogger<KeyManagementService>> _mockLogger;
        private KeyManagementService _keyManagementService;
        private byte[] _testMasterEncryptionKey;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockLogger = new Mock<ILogger<KeyManagementService>>();

            // Generate a consistent 32-byte key for testing AES-256
            _testMasterEncryptionKey = new byte[32];
            for (int i = 0; i < _testMasterEncryptionKey.Length; i++)
            {
                _testMasterEncryptionKey[i] = (byte)(i + 1); // Simple predictable pattern
            }

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            _mockMySqlConnectorWrapper.Setup(c => c.OpenConnectionAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));

            _keyManagementService = new KeyManagementService(
                _mockDbHelper.Object,
                _mockLogger.Object,
                _testMasterEncryptionKey
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

        #region RotateKeyAsync Tests

        [TestMethod]
        public async Task RotateKeyAsync_InsertsEncryptedKey()
        {
            // Arrange
            var expirationDate = DateTime.UtcNow.AddYears(1);
            byte[] capturedEncryptedKey = null;

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryAsync(
                    "spInsertKey",
                    CommandType.StoredProcedure,
                    It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(1) // Simulate 1 row affected
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual("spInsertKey", proc);
                    var secretKeyParam = pars.FirstOrDefault(p => p.ParameterName == "p_SecretKey");
                    Assert.IsNotNull(secretKeyParam);
                    capturedEncryptedKey = secretKeyParam.Value as byte[];
                    Assert.IsNotNull(capturedEncryptedKey);
                    Assert.IsTrue(capturedEncryptedKey.Length > 32, "Encrypted key should be longer than original due to IV and encryption overhead.");

                    var expirationDateParam = pars.FirstOrDefault(p => p.ParameterName == "p_ExpirationDate");
                    Assert.IsNotNull(expirationDateParam);
                    Assert.AreEqual(expirationDate, (DateTime)expirationDateParam.Value);
                });

            // Act
            await _keyManagementService.RotateKeyAsync(expirationDate);

            // Assert
            _mockMySqlConnectorWrapper.Verify(c => c.OpenConnectionAsync(), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("spInsertKey", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
            Assert.IsNotNull(capturedEncryptedKey);

            // Try to decrypt the captured key to verify it's plausible (optional deep check)
            // This uses the service's own private decryption logic implicitly
            using (var aes = Aes.Create())
            {
                aes.Key = _testMasterEncryptionKey;
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

            // Re-initialize service here for this test to ensure fresh state with specific mocks
            // _mockDbHelper = new Mock<IDbHelper>(); // Already in TestInitialize
            // _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>(); // Already in TestInitialize
            // _mockLogger = new Mock<ILogger<KeyManagementService>>(); // Already in TestInitialize
            // _keyManagementService = new KeyManagementService( // Already in TestInitialize
            //     _mockDbHelper.Object,
            //     _mockLogger.Object,
            //     _testMasterEncryptionKey 
            // );
            // The above re-initialization for the GetCurrentKeyAsync_ActiveKeyFound_ReturnsDecryptedKeyAsSecureString test
            // did not solve the NREs, so it's reverted to rely on TestInitialize.
            // The NREs point to _dbHelper or wrapper being null, which should be set by TestInitialize.

            byte[] originalPlainKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(originalPlainKey); }

            byte[] encryptedKeyFromDb;
            using (var aes = Aes.Create())
            {
                aes.Key = _testMasterEncryptionKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                using var encryptor = aes.CreateEncryptor();
                byte[] cipherText = encryptor.TransformFinalBlock(originalPlainKey, 0, originalPlainKey.Length);
                encryptedKeyFromDb = new byte[aes.IV.Length + cipherText.Length];
                Buffer.BlockCopy(aes.IV, 0, encryptedKeyFromDb, 0, aes.IV.Length);
                Buffer.BlockCopy(cipherText, 0, encryptedKeyFromDb, aes.IV.Length, cipherText.Length);
            }

            var mockReader = new Mock<IMySqlDataReaderWrapper>(); // Fresh mock reader for this test
            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteReaderAsync("spGetActiveKey", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockReader.Object);

            mockReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync(true)  // First call to ReadAsync, key found
                      .ReturnsAsync(false); // Subsequent calls

            mockReader.Setup(r => r["SecretKey"]).Returns(encryptedKeyFromDb);

            // Act
            var secureStringResult = await _keyManagementService.GetCurrentKeyAsync();

            // Assert
            Assert.IsNotNull(secureStringResult);
            var decryptedKeyString = SecureStringToString(secureStringResult);
            Assert.AreEqual(Convert.ToBase64String(originalPlainKey), decryptedKeyString);

            _mockMySqlConnectorWrapper.Verify(c => c.OpenConnectionAsync(), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("spGetActiveKey", CommandType.StoredProcedure, null), Times.Once);
            mockReader.Verify(r => r["SecretKey"], Times.Once);
        }

        [TestMethod]
        public async Task GetCurrentKeyAsync_NoActiveKeyFound_ReturnsNull()
        {
            // Arrange
            var mockReader = new Mock<IMySqlDataReaderWrapper>(); // Fresh mock reader
            //var mockReader = new Mock<IMySqlDataReaderWrapper>(); // Fresh mock reader
            //var mockReader = new Mock<IMySqlDataReaderWrapper>(); // Fresh mock reader
            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteReaderAsync("spGetActiveKey", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockReader.Object);

            mockReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync(false); // Simulate no records

            // Act
            var result = await _keyManagementService.GetCurrentKeyAsync();

            // Assert
            Assert.IsNull(result);
            _mockMySqlConnectorWrapper.Verify(c => c.OpenConnectionAsync(), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("spGetActiveKey", CommandType.StoredProcedure, null), Times.Once);
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No active key found in the database.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
        
        // or the CS1061 error: The issue arises because the `Setup` method is being incorrectly chained on the result of another `Setup` call.  
        // The correct approach is to call `Setup` directly on the mock object, not on the result of a `Setup` call.  

        [TestMethod]
        public async Task GetCurrentKeyAsync_DbReturnsNullKey_ReturnsNull()
        {
            // Arrange  
            var mockReader = new Mock<IMySqlDataReaderWrapper>();
            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteReaderAsync("spGetActiveKey", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockReader.Object); // Corrected: Removed the incorrect chaining of `Setup`.  

            mockReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync(true)
                      .ReturnsAsync(false);
            mockReader.Setup(r => r["SecretKey"]).Returns(null); // DB returns null for the key column  

            // Act  
            var result = await _keyManagementService.GetCurrentKeyAsync();

            // Assert  
            Assert.IsNull(result);
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No active key found in the database.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }


        [TestMethod]
        public async Task GetCurrentKeyAsync_DecryptionFails_ThrowsCryptographicException()
        {
            // Arrange
            byte[] invalidEncryptedKey = new byte[] { 1, 2, 3, 4, 5 }; // Too short to be valid AES encrypted data with IV

            var mockReader = new Mock<IMySqlDataReaderWrapper>();
            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteReaderAsync("spGetActiveKey", CommandType.StoredProcedure, null))
                .ReturnsAsync(mockReader.Object);

            mockReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync(true)
                      .ReturnsAsync(false);
            mockReader.Setup(r => r["SecretKey"]).Returns(invalidEncryptedKey);
            
            // Act & Assert
            // The actual exception might be CryptographicException or one of its derivatives like ArgumentException
            // depending on where the validation fails in Aes.CreateDecryptor or TransformFinalBlock.
            // For this test, we'll expect CryptographicException or a parent like SystemException if specific type is too volatile.
            await Assert.ThrowsExceptionAsync<CryptographicException>(async () =>
            {
                await _keyManagementService.GetCurrentKeyAsync();
            });
        }

        #endregion
    }
}
