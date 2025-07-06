using Moq;
using Microsoft.AspNetCore.DataProtection;
using GateKeeper.Server.Interface; 
using System.Text;
using System.Security.Cryptography; 
using GateKeeper.Server.Services.Site; 

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class StringDataProtectorWrapperTests
    {
        private Mock<IDataProtector> _mockDataProtector;
        private IStringDataProtector _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockDataProtector = new Mock<IDataProtector>();
            _service = new StringDataProtectorWrapper(_mockDataProtector.Object);
        }

        #region Protect Tests
        [TestMethod]
        public void Protect_ValidPlaintext_ReturnsBase64ProtectedString()
        {
            // Arrange
            var plaintext = "sensitive_data";
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var protectedBytes = Encoding.UTF8.GetBytes("protected_bytes_mock_result"); // Mocked result
            var expectedBase64Result = Convert.ToBase64String(protectedBytes);

            _mockDataProtector.Setup(dp => dp.Protect(It.Is<byte[]>(b => b.SequenceEqual(plaintextBytes))))
                              .Returns(protectedBytes);

            // Act
            var result = _service.Protect(plaintext);

            // Assert
            _mockDataProtector.Verify(dp => dp.Protect(It.Is<byte[]>(b => b.SequenceEqual(plaintextBytes))), Times.Once);
            Assert.AreEqual(expectedBase64Result, result);
        }

        [TestMethod]
        public void Protect_NullPlaintext_ReturnsNull()
        {
            // Act
            var result = _service.Protect(null);

            // Assert
            Assert.IsNull(result);
            _mockDataProtector.Verify(dp => dp.Protect(It.IsAny<byte[]>()), Times.Never);
        }

        [TestMethod]
        public void Protect_EmptyPlaintext_ReturnsBase64ProtectedStringOfEmptyProtectedBytes()
        {
            // Arrange
            var plaintext = "";
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext); // Empty byte array
            var protectedBytes = Encoding.UTF8.GetBytes("protected_empty_mock_result"); // Mocked result for empty
            var expectedBase64Result = Convert.ToBase64String(protectedBytes);

            _mockDataProtector.Setup(dp => dp.Protect(It.Is<byte[]>(b => b.SequenceEqual(plaintextBytes))))
                              .Returns(protectedBytes);
            
            // Act
            var result = _service.Protect(plaintext);

            // Assert
            _mockDataProtector.Verify(dp => dp.Protect(It.Is<byte[]>(b => b.SequenceEqual(plaintextBytes))), Times.Once);
            Assert.AreEqual(expectedBase64Result, result);
        }
        #endregion

        #region Unprotect Tests
        [TestMethod]
        public void Unprotect_ValidProtectedData_ReturnsPlaintext()
        {
            // Arrange
            var originalPlaintext = "sensitive_data";
            var unprotectedPayloadBytes = Encoding.UTF8.GetBytes(originalPlaintext);
            var protectedBytesInput = Encoding.UTF8.GetBytes("protected_bytes_mock_result");
            var base64ProtectedData = Convert.ToBase64String(protectedBytesInput);

            _mockDataProtector.Setup(dp => dp.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedBytesInput))))
                              .Returns(unprotectedPayloadBytes);

            // Act
            var result = _service.Unprotect(base64ProtectedData);

            // Assert
            _mockDataProtector.Verify(dp => dp.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedBytesInput))), Times.Once);
            Assert.AreEqual(originalPlaintext, result);
        }

        [TestMethod]
        public void Unprotect_NullProtectedData_ReturnsNull()
        {
            // Act
            var result = _service.Unprotect(null);

            // Assert
            Assert.IsNull(result);
            _mockDataProtector.Verify(dp => dp.Unprotect(It.IsAny<byte[]>()), Times.Never);
        }

        [TestMethod]
        public void Unprotect_EmptyProtectedData_ReturnsNullAfterCryptographicOrFormatIssues()
        {
            // Arrange
            var base64ProtectedData = ""; // Convert.FromBase64String("") is an empty byte array
            var emptyByteArray = Array.Empty<byte>();

            // If Unprotect is called with an empty byte array, it might throw CryptographicException
            _mockDataProtector.Setup(dp => dp.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(emptyByteArray))))
                              .Throws(new CryptographicException("Mock crypto error on empty array"));
            
            // Act
            var result = _service.Unprotect(base64ProtectedData);

            // Assert
            // SUT catches CryptographicException and returns null.
            // Convert.FromBase64String("") does NOT throw FormatException, it returns byte[0].
            Assert.IsNull(result);
            _mockDataProtector.Verify(dp => dp.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(emptyByteArray))), Times.Once);
        }

        [TestMethod]
        public void Unprotect_InvalidBase64ProtectedData_ReturnsNull()
        {
            // Arrange
            var invalidBase64ProtectedData = "invalid-base64-%";

            // Act
            var result = _service.Unprotect(invalidBase64ProtectedData);

            // Assert
            // SUT catches FormatException from Convert.FromBase64String and returns null
            Assert.IsNull(result);
            _mockDataProtector.Verify(dp => dp.Unprotect(It.IsAny<byte[]>()), Times.Never);
        }

        [TestMethod]
        public void Unprotect_DataCannotBeUnprotected_ReturnsNull()
        {
            // Arrange
            var protectedBytesInput = Encoding.UTF8.GetBytes("some_valid_looking_protected_bytes");
            var base64ProtectedData = Convert.ToBase64String(protectedBytesInput);

            _mockDataProtector.Setup(dp => dp.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedBytesInput))))
                              .Throws(new CryptographicException("Mock crypto error - tampered or wrong key"));
            
            // Act
            var result = _service.Unprotect(base64ProtectedData);

            // Assert
            // SUT catches CryptographicException and returns null
            Assert.IsNull(result);
            _mockDataProtector.Verify(dp => dp.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedBytesInput))), Times.Once);
        }
        #endregion
    }
}
