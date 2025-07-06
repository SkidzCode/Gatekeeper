using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Interface;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using GateKeeper.Server.Models.Account.UserModels;
using System; // For DateTime
using System.Linq;
using GateKeeper.Server.Services.Site; // For ToList

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class UserServiceTests
    {
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<ILogger<UserService>> _mockLogger;
        private UserService _userService;

        [TestInitialize]
        public void Setup()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _userService = new UserService(_mockUserRepository.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task RegisterUser_ShouldAddUserSuccessfully_WhenRepositoryReturnsSuccess()
        {
            // Arrange
            var userToAdd = new User { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe", Password = "password123", Phone = "1234567890" };
            int expectedUserId = 1;
            int successResultCode = 0; // 0 for success

            _mockUserRepository.Setup(repo => repo.AddUserAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync((expectedUserId, successResultCode));

            // Act
            var registrationResponse = await _userService.RegisterUser(userToAdd);

            // Assert
            Assert.IsTrue(registrationResponse.IsSuccessful);
            Assert.IsNotNull(registrationResponse.User);
            Assert.AreEqual(expectedUserId, registrationResponse.User.Id);
            Assert.AreEqual(userToAdd.FirstName, registrationResponse.User.FirstName);
            Assert.AreEqual(userToAdd.LastName, registrationResponse.User.LastName);
            Assert.AreEqual(userToAdd.Email, registrationResponse.User.Email);
            Assert.AreEqual(userToAdd.Username, registrationResponse.User.Username);
            Assert.AreEqual(string.Empty, registrationResponse.User.Password, "Password should be cleared in response for successful registration.");
            Assert.IsNull(registrationResponse.FailureReason);

            _mockUserRepository.Verify(repo => repo.AddUserAsync(
                It.Is<User>(u => u.Username == userToAdd.Username && u.Password != userToAdd.Password), // Password should be hashed
                It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task RegisterUser_ShouldFail_WhenEmailExists()
        {
            // Arrange
            var userToAdd = new User { Email = "exists@example.com", Username = "newuser", Password = "password" };
            int emailExistsCode = 1;
            _mockUserRepository.Setup(repo => repo.AddUserAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(((int?)null, emailExistsCode)); // No userId returned, specific result code

            // Act
            var response = await _userService.RegisterUser(userToAdd);

            // Assert
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual("Email already exists.", response.FailureReason);
            _mockUserRepository.Verify(repo => repo.AddUserAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        }


        [TestMethod]
        public async Task ChangePassword_ShouldSucceed_WhenRepositorySucceeds()
        {
            // Arrange
            int userId = 1;
            string newPassword = "newPassword123";
            _mockUserRepository.Setup(repo => repo.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ChangePassword(userId, newPassword);

            // Assert
            Assert.AreEqual(1, result); // 1 for success as per old contract
            _mockUserRepository.Verify(repo => repo.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task GetUserById_ShouldReturnUser_WhenRepositoryReturnsUser()
        {
            // Arrange
            int userId = 1;
            var expectedUser = new User { Id = userId, Username = "testuser", Roles = new List<string> { "User" } };
            _mockUserRepository.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync(expectedUser);

            // Act
            var result = await _userService.GetUser(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUser.Id, result.Id);
            Assert.AreEqual(expectedUser.Username, result.Username);
            Assert.AreEqual(expectedUser.Roles.Count, result.Roles.Count);
            _mockUserRepository.Verify(repo => repo.GetUserByIdAsync(userId), Times.Once);
        }

        [TestMethod]
        public async Task GetUserById_ShouldReturnNull_WhenRepositoryReturnsNull()
        {
            // Arrange
            int userId = 2;
            _mockUserRepository.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetUser(userId);

            // Assert
            Assert.IsNull(result);
            _mockUserRepository.Verify(repo => repo.GetUserByIdAsync(userId), Times.Once);
        }

        [TestMethod]
        public async Task GetUserByIdentifier_ShouldReturnUser_WhenRepositoryReturnsUser()
        {
            // Arrange
            string identifier = "testuser";
            var expectedUser = new User { Id = 1, Username = identifier, Roles = new List<string> { "Admin" } };
            _mockUserRepository.Setup(repo => repo.GetUserByIdentifierAsync(identifier)).ReturnsAsync(expectedUser);

            // Act
            var result = await _userService.GetUser(identifier);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUser.Username, result.Username);
            _mockUserRepository.Verify(repo => repo.GetUserByIdentifierAsync(identifier), Times.Once);
        }


        [TestMethod]
        public async Task UpdateUser_ShouldReturnUser_WhenRepositorySucceeds()
        {
            // Arrange
            var userToUpdate = new User { Id = 1, Username = "updateduser" };
            _mockUserRepository.Setup(repo => repo.UpdateUserAsync(userToUpdate)).ReturnsAsync(true);

            // Act
            var result = await _userService.UpdateUser(userToUpdate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userToUpdate.Username, result.Username); // Service returns the input user on success currently
            _mockUserRepository.Verify(repo => repo.UpdateUserAsync(userToUpdate), Times.Once);
        }

        [TestMethod]
        public async Task UpdateUser_ShouldStillReturnUser_WhenRepositoryReturnsFalse()
        {
            // Arrange
            var userToUpdate = new User { Id = 1, Username = "updateduser" };
            _mockUserRepository.Setup(repo => repo.UpdateUserAsync(userToUpdate)).ReturnsAsync(false);

            // Act
            var result = await _userService.UpdateUser(userToUpdate);

            // Assert
            // Current service implementation returns the input user even if repo indicates no rows affected.
            Assert.IsNotNull(result);
            Assert.AreEqual(userToUpdate.Username, result.Username);
            _mockUserRepository.Verify(repo => repo.UpdateUserAsync(userToUpdate), Times.Once);
        }


        [TestMethod]
        public async Task GetAllUsers_ShouldReturnAllUsers_FromRepository()
        {
            // Arrange
            var expectedUsers = new List<User>
            {
                new User { Id = 1, Username = "user1" },
                new User { Id = 2, Username = "user2" }
            };
            _mockUserRepository.Setup(repo => repo.GetAllUsersAsync()).ReturnsAsync(expectedUsers);

            // Act
            var result = await _userService.GetUsers();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUsers.Count, result.Count);
            Assert.IsTrue(result.Any(u => u.Username == "user1"));
            _mockUserRepository.Verify(repo => repo.GetAllUsersAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetRolesAsync_ShouldReturnRoles_FromRepository()
        {
            // Arrange
            int userId = 1;
            var expectedRoles = new List<string> { "Admin", "User" };
            _mockUserRepository.Setup(repo => repo.GetUserRolesAsync(userId)).ReturnsAsync(expectedRoles);

            // Act
            var result = await _userService.GetRolesAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRoles.Count, result.Count);
            CollectionAssert.AreEquivalent(expectedRoles, result);
            _mockUserRepository.Verify(repo => repo.GetUserRolesAsync(userId), Times.Once);
        }

        [TestMethod]
        public async Task UpdateUserRoles_ShouldCallRepository()
        {
            // Arrange
            int userId = 1;
            var rolesToUpdate = new List<string> { "SuperUser" };
            _mockUserRepository.Setup(repo => repo.UpdateUserRolesAsync(userId, rolesToUpdate)).Returns(Task.CompletedTask);

            // Act
            await _userService.UpdateUserRoles(userId, rolesToUpdate);

            // Assert
            _mockUserRepository.Verify(repo => repo.UpdateUserRolesAsync(userId, rolesToUpdate), Times.Once);
        }

        [TestMethod]
        public async Task UsernameExistsAsync_ShouldReturnTrue_WhenRepositoryReturnsTrue()
        {
            // Arrange
            string username = "existinguser";
            _mockUserRepository.Setup(repo => repo.UsernameExistsAsync(username)).ReturnsAsync(true);

            // Act
            bool exists = await _userService.UsernameExistsAsync(username);

            // Assert
            Assert.IsTrue(exists);
            _mockUserRepository.Verify(repo => repo.UsernameExistsAsync(username), Times.Once);
        }

        [TestMethod]
        public async Task EmailExistsAsync_ShouldReturnFalse_WhenRepositoryReturnsFalse()
        {
            // Arrange
            string email = "nonexisting@example.com";
            _mockUserRepository.Setup(repo => repo.EmailExistsAsync(email)).ReturnsAsync(false);

            // Act
            bool exists = await _userService.EmailExistsAsync(email);

            // Assert
            Assert.IsFalse(exists);
            _mockUserRepository.Verify(repo => repo.EmailExistsAsync(email), Times.Once);
        }
    }
}
