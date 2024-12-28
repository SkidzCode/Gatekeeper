// File: GateKeeper.Server.Test/Services/RoleServiceTests.cs
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class RoleServiceTests
    {
        private Mock<IDBHelper> _mockDbHelper;
        private Mock<ILogger<RoleService>> _mockLogger;
        private RoleService _roleService;

        [TestInitialize]
        public void Setup()
        {
            _mockDbHelper = new Mock<IDBHelper>();
            _mockLogger = new Mock<ILogger<RoleService>>();
            var mockConfiguration = new Mock<IConfiguration>();

            _roleService = new RoleService(mockConfiguration.Object, _mockDbHelper.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task AddRole_ShouldAddRoleSuccessfully()
        {
            // Arrange
            var roleToAdd = new Role { RoleName = "Administrator" };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteNonQueryAsync("InsertRole", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(1)
                .Callback<string, CommandType, MySqlParameter[]>((commandText, commandType, parameters) =>
                {
                    // You can add additional assertions here if needed
                    Assert.AreEqual("InsertRole", commandText);
                    Assert.AreEqual(CommandType.StoredProcedure, commandType);
                    Assert.IsTrue(parameters.Any(p => p.ParameterName == "@p_RoleName" && (string)p.Value == "Administrator"));
                });

            // Act
            var result = await _roleService.AddRole(roleToAdd);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Administrator", result.RoleName);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteNonQueryAsync("InsertRole", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }

        [TestMethod]
        public async Task GetRoleById_ShouldReturnRole()
        {
            // Arrange
            int roleId = 1;
            var expectedRole = new Role { Id = roleId, RoleName = "User" };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            var mockReader = new Mock<IMySqlDataReaderWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteReaderAsync("GetRoleById", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockReader.Object);

            // Setup reader to return a single role
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Only one invocation

            mockReader.Setup(r => r["Id"]).Returns(roleId);
            mockReader.Setup(r => r["RoleName"]).Returns("User");

            // Act
            var result = await _roleService.GetRoleById(roleId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRole.Id, result.Id);
            Assert.AreEqual(expectedRole.RoleName, result.RoleName);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteReaderAsync("GetRoleById", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && p[0].ParameterName == "@p_Id" && (int)p[0].Value == roleId)), Times.Once);
            mockReader.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Once); // Changed to Times.Once
        }

        [TestMethod]
        public async Task GetRoleById_ShouldReturnNull_WhenRoleNotFound()
        {
            // Arrange
            int roleId = 2;

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            var mockReader = new Mock<IMySqlDataReaderWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteReaderAsync("GetRoleById", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockReader.Object);

            // Setup reader to return no roles
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Only one invocation

            // Act
            var result = await _roleService.GetRoleById(roleId);

            // Assert
            Assert.IsNull(result);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteReaderAsync("GetRoleById", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && p[0].ParameterName == "@p_Id" && (int)p[0].Value == roleId)), Times.Once);
            mockReader.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Once); // Changed to Times.Once
        }

        [TestMethod]
        public async Task GetRoleByName_ShouldReturnRole()
        {
            // Arrange
            string roleName = "Manager";
            var expectedRole = new Role { Id = 3, RoleName = roleName };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            var mockReader = new Mock<IMySqlDataReaderWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteReaderAsync("GetRoleByName", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockReader.Object);

            // Setup reader to return a single role
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Only one invocation

            mockReader.Setup(r => r["Id"]).Returns(expectedRole.Id);
            mockReader.Setup(r => r["RoleName"]).Returns(expectedRole.RoleName);

            // Act
            var result = await _roleService.GetRoleByName(roleName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRole.Id, result.Id);
            Assert.AreEqual(expectedRole.RoleName, result.RoleName);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteReaderAsync("GetRoleByName", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && p[0].ParameterName == "@p_RoleName" && (string)p[0].Value == roleName)), Times.Once);
            mockReader.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Once); // Changed to Times.Once
        }

        [TestMethod]
        public async Task GetRoleByName_ShouldReturnNull_WhenRoleNotFound()
        {
            // Arrange
            string roleName = "NonExistentRole";

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            var mockReader = new Mock<IMySqlDataReaderWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteReaderAsync("GetRoleByName", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(mockReader.Object);

            // Setup reader to return no roles
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Only one invocation

            // Act
            var result = await _roleService.GetRoleByName(roleName);

            // Assert
            Assert.IsNull(result);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteReaderAsync("GetRoleByName", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && p[0].ParameterName == "@p_RoleName" && (string)p[0].Value == roleName)), Times.Once);
            mockReader.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Once); // Changed to Times.Once
        }

        [TestMethod]
        public async Task UpdateRole_ShouldUpdateRoleSuccessfully()
        {
            // Arrange
            var roleToUpdate = new Role { Id = 4, RoleName = "Supervisor" };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteNonQueryAsync("UpdateRole", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(1);

            // Act
            var result = await _roleService.UpdateRole(roleToUpdate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(roleToUpdate.Id, result.Id);
            Assert.AreEqual(roleToUpdate.RoleName, result.RoleName);

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteNonQueryAsync("UpdateRole", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 2 &&
                    p[0].ParameterName == "@p_Id" && (int)p[0].Value == roleToUpdate.Id &&
                    p[1].ParameterName == "@p_RoleName" && (string)p[1].Value == roleToUpdate.RoleName)), Times.Once);
        }

        [TestMethod]
        public async Task GetAllRoles_ShouldReturnAllRoles()
        {
            // Arrange
            var expectedRoles = new List<Role>
            {
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "User" },
                new Role { Id = 3, RoleName = "Guest" }
            };

            var mockWrapper = new Mock<IMySqlConnectorWrapper>();
            var mockReader = new Mock<IMySqlDataReaderWrapper>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(mockWrapper.Object);

            mockWrapper.Setup(wrapper => wrapper.ExecuteReaderAsync("GetAllRoles", CommandType.StoredProcedure))
                .ReturnsAsync(mockReader.Object);

            // Setup reader to return multiple roles
            var callCount = 0;
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => callCount < expectedRoles.Count)
                .Callback(() => { callCount++; });

            mockReader.Setup(r => r["Id"]).Returns(() => expectedRoles[callCount - 1].Id);
            mockReader.Setup(r => r["RoleName"]).Returns(() => expectedRoles[callCount - 1].RoleName);

            // Act
            var result = await _roleService.GetAllRoles();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRoles.Count, result.Count);
            for (int i = 0; i < expectedRoles.Count; i++)
            {
                Assert.AreEqual(expectedRoles[i].Id, result[i].Id);
                Assert.AreEqual(expectedRoles[i].RoleName, result[i].RoleName);
            }

            _mockDbHelper.Verify(db => db.GetWrapperAsync(), Times.Once);
            mockWrapper.Verify(wrapper => wrapper.ExecuteReaderAsync("GetAllRoles", CommandType.StoredProcedure), Times.Once);
            mockReader.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Exactly(expectedRoles.Count + 1));
        }
    }
}
