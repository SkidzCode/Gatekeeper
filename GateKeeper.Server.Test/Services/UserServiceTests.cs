// File: GateKeeper.Server.Test/Services/UserServiceTests.cs
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using GateKeeper.Server.Models.Account.User;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class UserServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<ILogger<UserService>> _mockLogger;
        private UserService _userService;

        [TestInitialize]
        public void Setup()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockLogger = new Mock<ILogger<UserService>>();
            var mockConfiguration = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();

            // Mock the configuration section to return a valid DatabaseConfig object
            mockSection.Setup(x => x["ConnectionString"]).Returns("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;");
            mockConfiguration.Setup(x => x.GetSection("DatabaseConfig")).Returns(mockSection.Object);

            _userService = new UserService(mockConfiguration.Object, _mockDbHelper.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task AddUser_ShouldAddUserSuccessfully()
        {
            // Arrange
            var userToAdd = new User { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe", Password = "password123", Phone = "1234567890" };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteNonQueryWithOutputAsync("AddUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(new Dictionary<string, object> { { "@p_ResultCode", 1 }, { "last_id", 1 } });

            // Act
            var (resultCode, resultUser) = await _userService.AddUser(userToAdd);

            // Assert
            Assert.AreEqual(1, resultCode);
            Assert.IsNotNull(resultUser);
            Assert.AreEqual(1, resultUser.Id);
            Assert.AreEqual("John", resultUser.FirstName);
            Assert.AreEqual("Doe", resultUser.LastName);
            Assert.AreEqual("john.doe@example.com", resultUser.Email);
            Assert.AreEqual("johndoe", resultUser.Username);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteNonQueryWithOutputAsync("AddUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }

        [TestMethod]
        public async Task GetUserById_ShouldReturnUser()
        {
            // Arrange
            int userId = 1;
            var expectedUser = new User
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Username = "johndoe",
                Phone = "1234567890",
                Password = "password123",
                Salt = "salt1",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            var mockUserReader = new Mock<IMySqlDataReaderWrapper>();
            var mockRolesReader = new Mock<IMySqlDataReaderWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteReaderAsync("GetUserProfile", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockUserReader.Object);

            mockUserReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockUserReader.Setup(r => r["Id"]).Returns(userId);
            mockUserReader.Setup(r => r["FirstName"]).Returns("John");
            mockUserReader.Setup(r => r["LastName"]).Returns("Doe");
            mockUserReader.Setup(r => r["Email"]).Returns("john.doe@example.com");
            mockUserReader.Setup(r => r["Username"]).Returns("johndoe");
            mockUserReader.Setup(r => r["Phone"]).Returns("1234567890");
            mockUserReader.Setup(r => r["Password"]).Returns("password123");
            mockUserReader.Setup(r => r["Salt"]).Returns("salt1");
            mockUserReader.Setup(r => r["IsActive"]).Returns(true);
            mockUserReader.Setup(r => r["CreatedAt"]).Returns(DateTime.Now);
            mockUserReader.Setup(r => r["UpdatedAt"]).Returns(DateTime.Now);

            // Mock roles reader to return no roles
            mockUserReader.SetupSequence(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true) // Move to roles result set
                .ReturnsAsync(false); // No roles

            mockRolesReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _userService.GetUser(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUser.Id, result.Id);
            Assert.AreEqual(expectedUser.FirstName, result.FirstName);
            Assert.AreEqual(expectedUser.LastName, result.LastName);
            Assert.AreEqual(expectedUser.Email, result.Email);
            Assert.AreEqual(expectedUser.Username, result.Username);
            Assert.AreEqual(expectedUser.Phone, result.Phone);
            Assert.AreEqual(expectedUser.Password, result.Password);
            Assert.AreEqual(expectedUser.Salt, result.Salt);
            Assert.AreEqual(expectedUser.IsActive, result.IsActive);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteReaderAsync("GetUserProfile", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => p.Length == 1 && p[0].ParameterName == "@p_UserId" && (int)p[0].Value == userId)), Times.Once);
            mockUserReader.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockUserReader.Verify(r => r.NextResultAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetUserById_ShouldReturnNull_WhenUserNotFound()
        {
            // Arrange
            int userId = 2;

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            var mockUserReader = new Mock<IMySqlDataReaderWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteReaderAsync("GetUserProfile", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockUserReader.Object);

            mockUserReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _userService.GetUser(userId);

            // Assert
            Assert.IsNull(result);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteReaderAsync("GetUserProfile", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => p.Length == 1 && p[0].ParameterName == "@p_UserId" && (int)p[0].Value == userId)), Times.Once);
            mockUserReader.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockUserReader.Verify(r => r.NextResultAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateUser_ShouldUpdateUserSuccessfully()
        {
            // Arrange
            var userToUpdate = new User
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Username = "johndoe",
                Phone = "1234567890",
                Password = "password123",
                Salt = "salt1",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteNonQueryAsync("UpdateUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(1);

            // Act
            var result = await _userService.UpdateUser(userToUpdate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userToUpdate.Id, result.Id);
            Assert.AreEqual(userToUpdate.FirstName, result.FirstName);
            Assert.AreEqual(userToUpdate.LastName, result.LastName);
            Assert.AreEqual(userToUpdate.Email, result.Email);
            Assert.AreEqual(userToUpdate.Username, result.Username);
            Assert.AreEqual(userToUpdate.Phone, result.Phone);
            Assert.AreEqual(userToUpdate.Password, result.Password);
            Assert.AreEqual(userToUpdate.Salt, result.Salt);
            Assert.AreEqual(userToUpdate.IsActive, result.IsActive);
            Assert.AreEqual(userToUpdate.CreatedAt, result.CreatedAt);
            Assert.AreEqual(userToUpdate.UpdatedAt, result.UpdatedAt);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            
        }

        [TestMethod]
        public async Task GetAllUsers_ShouldReturnAllUsers()
        {
            // Arrange
            var expectedUsers = new List<User>
            {
                new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe", Phone = "1234567890", Password = "password123", Salt = "salt1", IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new User { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Username = "janesmith", Phone = "0987654321", Password = "password456", Salt = "salt2", IsActive = false, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            var mockReader = new Mock<IMySqlDataReaderWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteReaderAsync("GetAllUsers", CommandType.StoredProcedure))
                .ReturnsAsync(mockReader.Object);

            var callCount = 0;
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => callCount < expectedUsers.Count)
                .Callback(() => { callCount++; });

            mockReader.Setup(r => r["Id"]).Returns(() => expectedUsers[callCount - 1].Id);
            mockReader.Setup(r => r["FirstName"]).Returns(() => expectedUsers[callCount - 1].FirstName);
            mockReader.Setup(r => r["LastName"]).Returns(() => expectedUsers[callCount - 1].LastName);
            mockReader.Setup(r => r["Email"]).Returns(() => expectedUsers[callCount - 1].Email);
            mockReader.Setup(r => r["Username"]).Returns(() => expectedUsers[callCount - 1].Username);
            mockReader.Setup(r => r["Phone"]).Returns(() => expectedUsers[callCount - 1].Phone);
            mockReader.Setup(r => r["Password"]).Returns(() => expectedUsers[callCount - 1].Password);
            mockReader.Setup(r => r["Salt"]).Returns(() => expectedUsers[callCount - 1].Salt);
            mockReader.Setup(r => r["IsActive"]).Returns(() => expectedUsers[callCount - 1].IsActive);
            mockReader.Setup(r => r["CreatedAt"]).Returns(() => expectedUsers[callCount - 1].CreatedAt);
            mockReader.Setup(r => r["UpdatedAt"]).Returns(() => expectedUsers[callCount - 1].UpdatedAt);

            // Act
            var result = await _userService.GetUsers();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUsers.Count, result.Count);
            for (int i = 0; i < expectedUsers.Count; i++)
            {
                Assert.AreEqual(expectedUsers[i].Id, result[i].Id);
                Assert.AreEqual(expectedUsers[i].FirstName, result[i].FirstName);
                Assert.AreEqual(expectedUsers[i].LastName, result[i].LastName);
                Assert.AreEqual(expectedUsers[i].Email, result[i].Email);
                Assert.AreEqual(expectedUsers[i].Username, result[i].Username);
                Assert.AreEqual(expectedUsers[i].Phone, result[i].Phone);
                Assert.AreEqual(expectedUsers[i].Password, result[i].Password);
                Assert.AreEqual(expectedUsers[i].Salt, result[i].Salt);
                Assert.AreEqual(expectedUsers[i].IsActive, result[i].IsActive);
                Assert.AreEqual(expectedUsers[i].CreatedAt, result[i].CreatedAt);
                Assert.AreEqual(expectedUsers[i].UpdatedAt, result[i].UpdatedAt);
            }

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteReaderAsync("GetAllUsers", CommandType.StoredProcedure), Times.Once);
            mockReader.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Exactly(expectedUsers.Count + 1));
        }
    }
}
