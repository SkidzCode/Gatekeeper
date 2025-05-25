using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account; // For VerifyTokenRequest
using GateKeeper.Server.Models.Account.Login; // For TokenVerificationResponse
using GateKeeper.Server.Models.Account.UserModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Data;
using MySqlConnector;
using System.Linq;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class VerifyTokenServiceTests
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<IMySqlConnectorWrapper> _mockMySqlConnectorWrapper;
        private Mock<IMySqlDataReaderWrapper> _mockDataReader;
        private Mock<ILogger<VerifyTokenService>> _mockLogger;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IUserService> _mockUserService;
        private VerifyTokenService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockDbHelper = new Mock<IDbHelper>();
            _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockDataReader = new Mock<IMySqlDataReaderWrapper>();
            _mockLogger = new Mock<ILogger<VerifyTokenService>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockUserService = new Mock<IUserService>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            _mockMySqlConnectorWrapper.Setup(c => c.OpenConnectionAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));

            _service = new VerifyTokenService(
                _mockConfiguration.Object,
                _mockDbHelper.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockUserService.Object
            );
        }

        // Helper to set up common reader fields for VerifyTokenAsync
        private void SetupMockReaderForTokenValidation(
            string sessionId, int userId, string verifyType, string salt, string hashedToken,
            bool isRevoked = false, bool isComplete = false,
            string firstName = "Test", string lastName = "User", string email = "test@example.com", 
            string phone = "123", string userSalt = "usersalt", string userPass = "userhash", string username = "testuser")
        {
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            _mockDataReader.Setup(r => r["Revoked"]).Returns(isRevoked);
            _mockDataReader.Setup(r => r["Complete"]).Returns(isComplete);
            _mockDataReader.Setup(r => r["VerifyType"]).Returns(verifyType);
            _mockDataReader.Setup(r => r["RefreshSalt"]).Returns(salt); // This is the token's salt (confusingly named RefreshSalt in SUT)
            _mockDataReader.Setup(r => r["HashedToken"]).Returns(hashedToken);
            _mockDataReader.Setup(r => r["UserId"]).Returns(userId);
            _mockDataReader.Setup(r => r["FirstName"]).Returns(firstName);
            _mockDataReader.Setup(r => r["LastName"]).Returns(lastName);
            _mockDataReader.Setup(r => r["Email"]).Returns(email);
            _mockDataReader.Setup(r => r["Phone"]).Returns(phone);
            _mockDataReader.Setup(r => r["Salt"]).Returns(userSalt); // User's main password salt
            _mockDataReader.Setup(r => r["Password"]).Returns(userPass); // User's main password hash
            _mockDataReader.Setup(r => r["Username"]).Returns(username);

            _mockUserService.Setup(us => us.GetRolesAsync(userId)).ReturnsAsync(new List<string> { "User" });
        }


        #region GenerateTokenAsync Tests
        [TestMethod]
        public async Task GenerateTokenAsync_ReturnsTokenInCorrectFormat()
        {
            // Arrange
            var userId = 1;
            var tokenType = "PasswordReset";
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync(
                "VerificationInsert", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(1); // Rows affected

            // Act
            var resultToken = await _service.GenerateTokenAsync(userId, tokenType);

            // Assert
            Assert.IsNotNull(resultToken);
            var parts = resultToken.Split('.');
            Assert.AreEqual(2, parts.Length, "Token should be in format 'tokenId.verifyToken'");
            Assert.IsTrue(Guid.TryParse(parts[0], out _), "Token ID part should be a GUID.");
            Assert.IsFalse(string.IsNullOrEmpty(parts[1]), "Verify token part should not be empty.");

            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync(
                "VerificationInsert",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p =>
                    (string)p.First(x => x.ParameterName == "@p_VerifyType").Value == tokenType &&
                    (int)p.First(x => x.ParameterName == "@p_UserId").Value == userId &&
                    !string.IsNullOrEmpty((string)p.First(x => x.ParameterName == "@p_Id").Value) && // TokenId (GUID)
                    !string.IsNullOrEmpty((string)p.First(x => x.ParameterName == "@p_HashedToken").Value) &&
                    !string.IsNullOrEmpty((string)p.First(x => x.ParameterName == "@p_Salt").Value) &&
                    ((DateTime)p.First(x => x.ParameterName == "@p_ExpiryDate").Value).Date == DateTime.UtcNow.AddDays(7).Date // Check if expiry is ~7 days
                )), Times.Once);
        }
        #endregion

        #region VerifyTokenAsync Tests

        [TestMethod]
        public async Task VerifyTokenAsync_ValidToken_ReturnsVerifiedResponse()
        {
            // Arrange
            var userId = 1;
            var tokenType = "PasswordReset";
            var rawTokenValue = "mySecretTokenValue"; // This is the part after the dot
            var salt = PasswordHelper.GenerateSalt();
            var hashedToken = PasswordHelper.HashPassword(rawTokenValue, salt);
            var sessionId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{sessionId}.{rawTokenValue}", TokenType = tokenType };

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("ValidateUser", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (string)p[0].Value == sessionId)))
                                      .ReturnsAsync(_mockDataReader.Object);
            SetupMockReaderForTokenValidation(sessionId, userId, tokenType, salt, hashedToken);
            
            // Act
            var response = await _service.VerifyTokenAsync(request);

            // Assert
            Assert.IsTrue(response.IsVerified);
            Assert.AreEqual(sessionId, response.SessionId);
            Assert.AreEqual(tokenType, response.TokenType);
            Assert.IsNotNull(response.User);
            Assert.AreEqual(userId, response.User.Id);
            Assert.IsNull(response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_InvalidTokenFormat_ReturnsNotVerified()
        {
            var request = new VerifyTokenRequest { VerificationCode = "invalidformat", TokenType = "Test" };
            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Invalid token format", response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_SessionIdNotFound_ReturnsNotVerified()
        {
            var sessionId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{sessionId}.token", TokenType = "Test" };
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("ValidateUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false); // Simulate not found

            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Invalid Session Id", response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_TokenRevoked_ReturnsNotVerified()
        {
            var sessionId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{sessionId}.token", TokenType = "Test" };
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("ValidateUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            SetupMockReaderForTokenValidation(sessionId, 1, "Test", "salt", "hashed", isRevoked: true);
            
            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Token already revoked", response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_TokenCompleted_ReturnsNotVerified()
        {
            var sessionId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{sessionId}.token", TokenType = "Test" };
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("ValidateUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            SetupMockReaderForTokenValidation(sessionId, 1, "Test", "salt", "hashed", isComplete: true);
            
            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Token already completed", response.FailureReason);
        }
        
        [TestMethod]
        public async Task VerifyTokenAsync_TokenTypeMismatch_ReturnsNotVerified()
        {
            var sessionId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{sessionId}.token", TokenType = "WrongType" };
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("ValidateUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            SetupMockReaderForTokenValidation(sessionId, 1, "CorrectType", "salt", "hashed");
            
            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Incorrect token type", response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_TokenHashMismatch_ReturnsNotVerified()
        {
            var userId = 1;
            var tokenType = "PasswordReset";
            var rawTokenValue = "mySecretTokenValue";
            var salt = PasswordHelper.GenerateSalt();
            var correctHashedToken = PasswordHelper.HashPassword(rawTokenValue, salt);
            var wrongHashedToken = PasswordHelper.HashPassword("wrongValue", salt); // Different hash
            var sessionId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{sessionId}.{rawTokenValue}", TokenType = tokenType };

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("ValidateUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            SetupMockReaderForTokenValidation(sessionId, userId, tokenType, salt, wrongHashedToken); // Setup with wrong hash
            
            var response = await _service.VerifyTokenAsync(request);

            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Invalid token", response.FailureReason);
            _mockUserService.Verify(us => us.GetRolesAsync(userId), Times.Once); // User details are fetched before hash check
        }
        #endregion

        #region RevokeTokensAsync Tests
        [TestMethod]
        public async Task RevokeTokensAsync_CallsSPAndReturnsRowsAffected()
        {
            var userId = 1;
            var tokenType = "Session";
            var token = "tokenId.tokenValue";
            var expectedRowsAffected = 1;

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync(
                "RevokeVerifyToken", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(expectedRowsAffected)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual("RevokeVerifyToken", proc);
                    Assert.AreEqual(userId, pars.First(p => p.ParameterName == "@p_UserId").Value);
                    Assert.AreEqual(tokenType, pars.First(p => p.ParameterName == "@p_VerifyType").Value);
                    Assert.AreEqual("tokenId", pars.First(p => p.ParameterName == "@p_TokenId").Value); // Check tokenId extraction
                    var outputParam = pars.First(p => p.ParameterName == "@p_RowsAffected" && p.Direction == ParameterDirection.Output);
                    outputParam.Value = expectedRowsAffected; // Simulate output param
                });

            var result = await _service.RevokeTokensAsync(userId, tokenType, token);
            Assert.AreEqual(expectedRowsAffected, result);
        }
        
        [TestMethod]
        public async Task RevokeTokensAsync_NullToken_CallsSPWithNullTokenId()
        {
            var userId = 1;
            var tokenType = "Session";
            var expectedRowsAffected = 1;

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync(
                "RevokeVerifyToken", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(expectedRowsAffected)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual(DBNull.Value, pars.First(p => p.ParameterName == "@p_TokenId").Value);
                    var outputParam = pars.First(p => p.ParameterName == "@p_RowsAffected" && p.Direction == ParameterDirection.Output);
                    outputParam.Value = expectedRowsAffected; 
                });

            var result = await _service.RevokeTokensAsync(userId, tokenType, null); // Pass null token
            Assert.AreEqual(expectedRowsAffected, result);
        }

        #endregion

        #region CompleteTokensAsync Tests
        [TestMethod]
        public async Task CompleteTokensAsync_CallsSPAndReturnsRowsAffected()
        {
            var userId = 1;
            var tokenType = "PasswordReset";
            var token = "tokenId.tokenValue";
            var expectedRowsAffected = 1;

             _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync(
                "CompleteVerifyToken", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(expectedRowsAffected)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual("CompleteVerifyToken", proc);
                    Assert.AreEqual(userId, pars.First(p => p.ParameterName == "@p_UserId").Value);
                    Assert.AreEqual(tokenType, pars.First(p => p.ParameterName == "@p_VerifyType").Value);
                    Assert.AreEqual("tokenId", pars.First(p => p.ParameterName == "@p_TokenId").Value);
                    var outputParam = pars.First(p => p.ParameterName == "@p_RowsAffected" && p.Direction == ParameterDirection.Output);
                    outputParam.Value = expectedRowsAffected; 
                });

            var result = await _service.CompleteTokensAsync(userId, tokenType, token);
            Assert.AreEqual(expectedRowsAffected, result);
        }
        #endregion
    }
}
