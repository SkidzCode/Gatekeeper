using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Interface; // Added for IRoleRepository
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using GateKeeper.Server.Services.Site;
// Dapper, System.Data, System.Linq, Moq.Dapper might not be needed here anymore or less used.

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class RoleServiceTests
    {
        private Mock<IRoleRepository> _mockRoleRepository;
        private Mock<ILogger<RoleService>> _mockLogger;
        private RoleService _roleService;

        [TestInitialize]
        public void Setup()
        {
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockLogger = new Mock<ILogger<RoleService>>();
            _roleService = new RoleService(_mockRoleRepository.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task AddRole_ShouldAddRoleSuccessfully()
        {
            // Arrange
            var roleToAdd = new Role { RoleName = "Administrator" };
            var expectedRoleAfterAdd = new Role { Id = 123, RoleName = "Administrator" }; // Assuming repository returns the role with ID

            _mockRoleRepository.Setup(repo => repo.AddRoleAsync(roleToAdd))
                .ReturnsAsync(expectedRoleAfterAdd);

            // Act
            var result = await _roleService.AddRole(roleToAdd);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRoleAfterAdd.RoleName, result.RoleName);
            Assert.AreEqual(expectedRoleAfterAdd.Id, result.Id);
            _mockRoleRepository.Verify(repo => repo.AddRoleAsync(roleToAdd), Times.Once());
        }


        [TestMethod]
        public async Task GetRoleById_ShouldReturnRole()
        {
            // Arrange
            int roleId = 1;
            var expectedRole = new Role { Id = roleId, RoleName = "User" };

            _mockRoleRepository.Setup(repo => repo.GetRoleByIdAsync(roleId))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _roleService.GetRoleById(roleId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRole.Id, result.Id);
            Assert.AreEqual(expectedRole.RoleName, result.RoleName);
            _mockRoleRepository.Verify(repo => repo.GetRoleByIdAsync(roleId), Times.Once());
        }

        [TestMethod]
        public async Task GetRoleById_ShouldReturnNull_WhenRoleNotFound()
        {
            // Arrange
            int roleId = 2;

            _mockRoleRepository.Setup(repo => repo.GetRoleByIdAsync(roleId))
                .ReturnsAsync((Role?)null);

            // Act
            var result = await _roleService.GetRoleById(roleId);

            // Assert
            Assert.IsNull(result);
            _mockRoleRepository.Verify(repo => repo.GetRoleByIdAsync(roleId), Times.Once());
        }

        [TestMethod]
        public async Task GetRoleByName_ShouldReturnRole()
        {
            // Arrange
            string roleName = "Manager";
            var expectedRole = new Role { Id = 3, RoleName = roleName };

            _mockRoleRepository.Setup(repo => repo.GetRoleByNameAsync(roleName))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _roleService.GetRoleByName(roleName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRole.Id, result.Id);
            Assert.AreEqual(expectedRole.RoleName, result.RoleName);
            _mockRoleRepository.Verify(repo => repo.GetRoleByNameAsync(roleName), Times.Once());
        }

        [TestMethod]
        public async Task GetRoleByName_ShouldReturnNull_WhenRoleNotFound()
        {
            // Arrange
            string roleName = "NonExistentRole";

            _mockRoleRepository.Setup(repo => repo.GetRoleByNameAsync(roleName))
                .ReturnsAsync((Role?)null);

            // Act
            var result = await _roleService.GetRoleByName(roleName);

            // Assert
            Assert.IsNull(result);
            _mockRoleRepository.Verify(repo => repo.GetRoleByNameAsync(roleName), Times.Once());
        }

        [TestMethod]
        public async Task UpdateRole_ShouldUpdateRoleSuccessfully()
        {
            // Arrange
            var roleToUpdate = new Role { Id = 4, RoleName = "Supervisor" };

            _mockRoleRepository.Setup(repo => repo.UpdateRoleAsync(roleToUpdate))
                .ReturnsAsync(roleToUpdate); // Assuming repository returns the updated role

            // Act
            var result = await _roleService.UpdateRole(roleToUpdate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(roleToUpdate.Id, result.Id);
            Assert.AreEqual(roleToUpdate.RoleName, result.RoleName);
            _mockRoleRepository.Verify(repo => repo.UpdateRoleAsync(roleToUpdate), Times.Once());
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

            _mockRoleRepository.Setup(repo => repo.GetAllRolesAsync())
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
            _mockRoleRepository.Verify(repo => repo.GetAllRolesAsync(), Times.Once());
        }
    }
}