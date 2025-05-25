using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using GateKeeper.Server.Models.Configuration; // For Convert

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class PasswordHelperTests
    {
        private PasswordSettingsConfig _passwordSettingsConfig;
        #region GenerateSalt Tests
        [TestMethod]
        public void GenerateSalt_ReturnsNonNullOrEmptyString()
        {
            var salt = PasswordHelper.GenerateSalt();
            Assert.IsFalse(string.IsNullOrEmpty(salt));
        }

        [TestMethod]
        public void GenerateSalt_ReturnsCorrectBase64LengthFor16ByteSalt()
        {
            // 16 bytes of data = 128 bits.
            // Base64 encoding uses 3 bytes of binary data to 4 characters.
            // (16 / 3) * 4 = 21.333... so it will be padded to 24 characters.
            var salt = PasswordHelper.GenerateSalt();
            Assert.AreEqual(24, salt.Length, "A 16-byte salt should result in a 24-character Base64 string.");

            // Also check if it's a valid Base64 string
            try
            {
                Convert.FromBase64String(salt);
            }
            catch (FormatException)
            {
                Assert.Fail("Generated salt is not a valid Base64 string.");
            }
        }

        [TestMethod]
        public void GenerateSalt_ReturnsUniqueSaltsAcrossCalls()
        {
            var saltList = new List<string>();
            for (int i = 0; i < 100; i++) // Generate a few salts
            {
                saltList.Add(PasswordHelper.GenerateSalt());
            }
            Assert.AreEqual(saltList.Count, saltList.Distinct().Count(), "Generated salts should be unique.");
        }
        #endregion

        #region HashPassword Tests
        [TestMethod]
        public void HashPassword_ReturnsNonNullOrEmptyHash()
        {
            var salt = PasswordHelper.GenerateSalt();
            var hash = PasswordHelper.HashPassword("password123", salt);
            Assert.IsFalse(string.IsNullOrEmpty(hash));
        }

        [TestMethod]
        public void HashPassword_ReturnsCorrectBase64LengthFor32ByteHash()
        {
            // 32 bytes of data = 256 bits.
            // (32 / 3) * 4 = 42.666... so it will be padded to 44 characters.
            var salt = PasswordHelper.GenerateSalt();
            var hash = PasswordHelper.HashPassword("password123", salt);
            Assert.AreEqual(44, hash.Length, "A 32-byte hash (SHA512 output from SUT) should result in a 44-character Base64 string.");
            // Also check if it's a valid Base64 string
            try
            {
                Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                Assert.Fail("Generated hash is not a valid Base64 string.");
            }
        }

        [TestMethod]
        public void HashPassword_IsDeterministicWithSameInputs()
        {
            var password = "mySecurePassword";
            var salt = PasswordHelper.GenerateSalt();
            var hash1 = PasswordHelper.HashPassword(password, salt);
            var hash2 = PasswordHelper.HashPassword(password, salt);
            Assert.AreEqual(hash1, hash2, "Hashes should be the same for the same password and salt.");
        }

        [TestMethod]
        public void HashPassword_DifferentSaltYieldsDifferentHash()
        {
            var password = "mySecurePassword";
            var salt1 = PasswordHelper.GenerateSalt();
            var salt2 = PasswordHelper.GenerateSalt();
            // Ensure salts are actually different for the test to be meaningful, though GenerateSalt should ensure this.
            while(salt1 == salt2) salt2 = PasswordHelper.GenerateSalt();

            var hash1 = PasswordHelper.HashPassword(password, salt1);
            var hash2 = PasswordHelper.HashPassword(password, salt2);
            Assert.AreNotEqual(hash1, hash2, "Hashes should be different for different salts.");
        }

        [TestMethod]
        public void HashPassword_DifferentPasswordYieldsDifferentHash()
        {
            var passwordA = "mySecurePasswordA";
            var passwordB = "mySecurePasswordB";
            var salt = PasswordHelper.GenerateSalt();
            var hashA = PasswordHelper.HashPassword(passwordA, salt);
            var hashB = PasswordHelper.HashPassword(passwordB, salt);
            Assert.AreNotEqual(hashA, hashB, "Hashes should be different for different passwords.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void HashPassword_NullPassword_ThrowsArgumentNullException()
        {
            var salt = PasswordHelper.GenerateSalt();
            // PasswordHelper.HashPassword internally uses Encoding.UTF8.GetBytes(password)
            // which will throw ArgumentNullException if password is null.
            PasswordHelper.HashPassword(null, salt);
        }

        [TestMethod]
        public void HashPassword_EmptyPassword_DoesNotThrowAndProducesHash()
        {
            var salt = PasswordHelper.GenerateSalt();
            var hash = PasswordHelper.HashPassword("", salt); // Empty password is a valid input
            Assert.IsFalse(string.IsNullOrEmpty(hash));
        }


        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void HashPassword_InvalidSalt_ThrowsFormatException()
        {
            // Convert.FromBase64String(salt) will throw FormatException for invalid Base64.
            PasswordHelper.HashPassword("password", "this is not valid base64 salt***"); // Added invalid chars
        }
        #endregion

        private void SetupPasswordPolicy(int requiredLength, bool requireUppercase, bool requireLowercase, bool requireDigit, bool requireNonAlphanumeric, int maxFailedAccessAttempts)
        {
            _passwordSettingsConfig = new PasswordSettingsConfig
            {
                RequiredLength = requiredLength,
                RequireUppercase = requireUppercase,
                RequireLowercase = requireLowercase,
                RequireDigit = requireDigit,
                RequireNonAlphanumeric = requireNonAlphanumeric,
                MaxFailedAccessAttempts = maxFailedAccessAttempts
            };
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_ValidPassword_ReturnsTrue()
        {
            SetupPasswordPolicy(8, true, true, true, true, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "ValidP@ss1");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_TooShort_ReturnsFalse()
        {
            SetupPasswordPolicy(8, true, true, true, true, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "V@lid1"); // 6 chars  
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_MissingUppercase_ReturnsFalse()
        {
            SetupPasswordPolicy(8, true, true, true, true, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "validp@ss1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_MissingUppercase_NotRequired_ReturnsTrue()
        {
            SetupPasswordPolicy(8, false, true, true, true, 5); // Uppercase not required  
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "validp@ss1");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_MissingLowercase_ReturnsFalse()
        {
            SetupPasswordPolicy(8, true, true, true, true, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "VALIDP@SS1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_MissingDigit_ReturnsFalse()
        {
            SetupPasswordPolicy(8, true, true, true, true, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "ValidP@ss");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_MissingSpecialChar_ReturnsFalse()
        {
            SetupPasswordPolicy(8, true, true, true, true, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "ValidPass1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_EmptyPassword_ReturnsFalse()
        {
            SetupPasswordPolicy(8, false, false, false, false, 5); // Lenient policy  
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_NullPassword_ReturnsFalse()
        {
            SetupPasswordPolicy(8, false, false, false, false, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, null);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_AllRequirementsDisabled_ShortPasswordIsValid()
        {
            // Policy: MinLength 3, no other requirements  
            SetupPasswordPolicy(3, false, false, false, false, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "abc");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ValidatePasswordStrengthAsync_OnlyLengthRequired_FailsIfTooShort()
        {
            SetupPasswordPolicy(3, false, false, false, false, 5);
            var result = await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettingsConfig, "ab");
            Assert.IsFalse(result);
        }
    }
}
