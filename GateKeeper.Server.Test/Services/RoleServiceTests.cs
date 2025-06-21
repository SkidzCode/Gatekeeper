using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using System.Linq;
using Moq.Dapper;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class RoleServiceTests
    {
        private Mock<IDbConnection> _mockDbConnection;
        private Mock<ILogger<RoleService>> _mockLogger;
        private RoleService _roleService;

        [TestInitialize]
        public void Setup()
        {
            _mockDbConnection = new Mock<IDbConnection>();
            _mockLogger = new Mock<ILogger<RoleService>>();
            _roleService = new RoleService(_mockDbConnection.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task AddRole_ShouldAddRoleSuccessfully()
        {
            // Arrange
            var roleToAdd = new Role { RoleName = "Administrator" };
            var expectedRoleId = 123; // Example ID

            _mockDbConnection.SetupDapper(c => c.QuerySingleAsync<int>("InsertRole", It.IsAny<object>(), null, null, CommandType.StoredProcedure))
                .ReturnsAsync(expectedRoleId);

            // Act
            var result = await _roleService.AddRole(roleToAdd);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Administrator", result.RoleName);
            Assert.AreEqual(expectedRoleId, result.Id); // Verify the ID is set
            // Verify Dapper's QuerySingleAsync was called with the correct parameters
            _mockDbConnection.Verify(c => c.QuerySingleAsync<int>("InsertRole", It.Is<DynamicParameters>(p => p.Get<string>("@p_RoleName") == "Administrator"), null, null, CommandType.StoredProcedure), Times.Once());
        }


        [TestMethod]
        public async Task GetRoleById_ShouldReturnRole()
        {
            // Arrange
            int roleId = 1;
            var expectedRole = new Role { Id = roleId, RoleName = "User" };

            _mockDbConnection.SetupDapper(c => c.QueryFirstOrDefaultAsync<Role>("GetRoleById", It.IsAny<object>(), null, null, CommandType.StoredProcedure))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _roleService.GetRoleById(roleId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRole.Id, result.Id);
            Assert.AreEqual(expectedRole.RoleName, result.RoleName);
            _mockDbConnection.Verify(c => c.QueryFirstOrDefaultAsync<Role>("GetRoleById", It.Is<DynamicParameters>(p => p.Get<int>("@p_Id") == roleId), null, null, CommandType.StoredProcedure), Times.Once());
        }

        [TestMethod]
        public async Task GetRoleById_ShouldReturnNull_WhenRoleNotFound()
        {
            // Arrange
            int roleId = 2;

            _mockDbConnection.SetupDapper(c => c.QueryFirstOrDefaultAsync<Role>("GetRoleById", It.IsAny<object>(), null, null, CommandType.StoredProcedure))
                .ReturnsAsync((Role)null);

            // Act
            var result = await _roleService.GetRoleById(roleId);

            // Assert
            Assert.IsNull(result);
            _mockDbConnection.Verify(c => c.QueryFirstOrDefaultAsync<Role>("GetRoleById", It.Is<DynamicParameters>(p => p.Get<int>("@p_Id") == roleId), null, null, CommandType.StoredProcedure), Times.Once());
        }

        [TestMethod]
        public async Task GetRoleByName_ShouldReturnRole()
        {
            // Arrange
            string roleName = "Manager";
            var expectedRole = new Role { Id = 3, RoleName = roleName };

            _mockDbConnection.SetupDapper(c => c.QueryFirstOrDefaultAsync<Role>("GetRoleByName", It.IsAny<object>(), null, null, CommandType.StoredProcedure))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _roleService.GetRoleByName(roleName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRole.Id, result.Id);
            Assert.AreEqual(expectedRole.RoleName, result.RoleName);
            _mockDbConnection.Verify(c => c.QueryFirstOrDefaultAsync<Role>("GetRoleByName", It.Is<DynamicParameters>(p => p.Get<string>("@p_RoleName") == roleName), null, null, CommandType.StoredProcedure), Times.Once());
        }

        [TestMethod]
        public async Task GetRoleByName_ShouldReturnNull_WhenRoleNotFound()
        {
            // Arrange
            string roleName = "NonExistentRole";

            _mockDbConnection.SetupDapper(c => c.QueryFirstOrDefaultAsync<Role>("GetRoleByName", It.IsAny<object>(), null, null, CommandType.StoredProcedure))
                .ReturnsAsync((Role)null);

            // Act
            var result = await _roleService.GetRoleByName(roleName);

            // Assert
            Assert.IsNull(result);
            _mockDbConnection.Verify(c => c.QueryFirstOrDefaultAsync<Role>("GetRoleByName", It.Is<DynamicParameters>(p => p.Get<string>("@p_RoleName") == roleName), null, null, CommandType.StoredProcedure), Times.Once());
        }

        [TestMethod]
        public async Task UpdateRole_ShouldUpdateRoleSuccessfully()
        {
            // Arrange
            var roleToUpdate = new Role { Id = 4, RoleName = "Supervisor" };

            _mockDbConnection.SetupDapper(c => c.ExecuteAsync("UpdateRole", It.IsAny<object>(), null, null, CommandType.StoredProcedure))
                .ReturnsAsync(1);

            // Act
            var result = await _roleService.UpdateRole(roleToUpdate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(roleToUpdate.Id, result.Id);
            Assert.AreEqual(roleToUpdate.RoleName, result.RoleName);
            _mockDbConnection.Verify(c => c.ExecuteAsync("UpdateRole", It.Is<DynamicParameters>(p => p.Get<int>("@p_Id") == roleToUpdate.Id && p.Get<string>("@p_RoleName") == roleToUpdate.RoleName), null, null, CommandType.StoredProcedure), Times.Once());
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

            _mockDbConnection.SetupDapper(c => c.QueryAsync<Role>("GetAllRoles", null, null, null, CommandType.StoredProcedure))
                .ReturnsAsync(expectedRoles);

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
            _mockDbConnection.Verify(c => c.QueryAsync<Role>("GetAllRoles", null, null, null, CommandType.StoredProcedure), Times.Once());
        }
    }
}