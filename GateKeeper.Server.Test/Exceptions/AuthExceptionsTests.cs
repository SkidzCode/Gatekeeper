using Microsoft.VisualStudio.TestTools.UnitTesting;
using GateKeeper.Server.Exceptions;
using System;

namespace GateKeeper.Server.Test.Exceptions
{
    [TestClass]
    public class AuthExceptionsTests
    {
        private const string TestErrorMessage = "This is a test error message.";
        private const string InnerExceptionMessage = "Inner test exception.";

        // Tests for InvalidCredentialsException
        [TestMethod]
        public void InvalidCredentialsException_ConstructorWithMessage_SetsMessageCorrectly()
        {
            // Arrange
            var exception = new InvalidCredentialsException(TestErrorMessage);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void InvalidCredentialsException_ConstructorWithMessageAndInnerException_SetsMessageAndInnerExceptionCorrectly()
        {
            // Arrange
            var innerException = new Exception(InnerExceptionMessage);
            var exception = new InvalidCredentialsException(TestErrorMessage, innerException);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual(InnerExceptionMessage, exception.InnerException.Message);
        }

        // Tests for UserNotFoundException
        [TestMethod]
        public void UserNotFoundException_ConstructorWithMessage_SetsMessageCorrectly()
        {
            // Arrange
            var exception = new UserNotFoundException(TestErrorMessage);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void UserNotFoundException_ConstructorWithMessageAndInnerException_SetsMessageAndInnerExceptionCorrectly()
        {
            // Arrange
            var innerException = new Exception(InnerExceptionMessage);
            var exception = new UserNotFoundException(TestErrorMessage, innerException);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual(InnerExceptionMessage, exception.InnerException.Message);
        }

        // Tests for AccountLockedException
        [TestMethod]
        public void AccountLockedException_ConstructorWithMessage_SetsMessageCorrectly()
        {
            // Arrange
            var exception = new AccountLockedException(TestErrorMessage);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void AccountLockedException_ConstructorWithMessageAndInnerException_SetsMessageAndInnerExceptionCorrectly()
        {
            // Arrange
            var innerException = new Exception(InnerExceptionMessage);
            var exception = new AccountLockedException(TestErrorMessage, innerException);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual(InnerExceptionMessage, exception.InnerException.Message);
        }

        // Tests for SessionExpiredException
        [TestMethod]
        public void SessionExpiredException_ConstructorWithMessage_SetsMessageCorrectly()
        {
            // Arrange
            var exception = new SessionExpiredException(TestErrorMessage);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void SessionExpiredException_ConstructorWithMessageAndInnerException_SetsMessageAndInnerExceptionCorrectly()
        {
            // Arrange
            var innerException = new Exception(InnerExceptionMessage);
            var exception = new SessionExpiredException(TestErrorMessage, innerException);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual(InnerExceptionMessage, exception.InnerException.Message);
        }

        // Tests for InvalidTokenException
        [TestMethod]
        public void InvalidTokenException_ConstructorWithMessage_SetsMessageCorrectly()
        {
            // Arrange
            var exception = new InvalidTokenException(TestErrorMessage);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void InvalidTokenException_ConstructorWithMessageAndInnerException_SetsMessageAndInnerExceptionCorrectly()
        {
            // Arrange
            var innerException = new Exception(InnerExceptionMessage);
            var exception = new InvalidTokenException(TestErrorMessage, innerException);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual(InnerExceptionMessage, exception.InnerException.Message);
        }

        // Tests for RegistrationException
        [TestMethod]
        public void RegistrationException_ConstructorWithMessage_SetsMessageCorrectly()
        {
            // Arrange
            var exception = new RegistrationException(TestErrorMessage);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void RegistrationException_ConstructorWithMessageAndInnerException_SetsMessageAndInnerExceptionCorrectly()
        {
            // Arrange
            var innerException = new Exception(InnerExceptionMessage);
            var exception = new RegistrationException(TestErrorMessage, innerException);

            // Assert
            Assert.AreEqual(TestErrorMessage, exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual(InnerExceptionMessage, exception.InnerException.Message);
        }
    }
}
