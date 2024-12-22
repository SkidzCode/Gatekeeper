using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for managing security-related features in the user management system.
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Enables Multi-Factor Authentication (MFA) for a user account.
        /// </summary>
        /// <param name="userId">The ID of the user enabling MFA.</param>
        /// <param name="mfaType">The type of MFA (e.g., SMS, email, or authenticator app).</param>
        /// <returns>Whether MFA was successfully enabled.</returns>
        Task<bool> EnableMfaAsync(int userId, MfaType mfaType);

        /// <summary>
        /// Generates backup codes for MFA recovery.
        /// </summary>
        /// <param name="userId">The ID of the user requesting backup codes.</param>
        /// <returns>A list of backup codes.</returns>
        Task<IEnumerable<string>> GenerateMfaBackupCodesAsync(int userId);

        /// <summary>
        /// Locks a user account after a set number of failed login attempts.
        /// </summary>
        /// <param name="userId">The ID of the user to lock.</param>
        /// <returns>Whether the account was successfully locked.</returns>
        Task<bool> LockAccountAsync(int userId);

        /// <summary>
        /// Adds CAPTCHA verification for suspicious login attempts.
        /// </summary>
        /// <param name="captchaResponse">The user's CAPTCHA response.</param>
        /// <returns>Whether the CAPTCHA was successfully validated.</returns>
        Task<bool> ValidateCaptchaAsync(string captchaResponse);

        /// <summary>
        /// Adds an IP address to the whitelist.
        /// </summary>
        /// <param name="ipAddress">The IP address to whitelist.</param>
        /// <returns>Whether the IP was successfully added.</returns>
        Task<bool> AddIpToWhitelistAsync(string ipAddress);

        /// <summary>
        /// Adds an IP address to the blacklist.
        /// </summary>
        /// <param name="ipAddress">The IP address to blacklist.</param>
        /// <returns>Whether the IP was successfully added.</returns>
        Task<bool> AddIpToBlacklistAsync(string ipAddress);

        /// <summary>
        /// Detects and blocks suspicious IP addresses.
        /// </summary>
        /// <param name="ipAddress">The suspicious IP address.</param>
        /// <returns>Whether the IP was successfully blocked.</returns>
        Task<bool> BlockSuspiciousIpAsync(string ipAddress);

        /// <summary>
        /// Tracks and retrieves security-related audit logs.
        /// </summary>
        /// <param name="userId">The ID of the user to retrieve logs for (optional).</param>
        /// <returns>A list of audit logs.</returns>
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int? userId = null);

        /// <summary>
        /// Hashes a password using a secure algorithm.
        /// </summary>
        /// <param name="password">The plain-text password to hash.</param>
        /// <returns>The hashed password.</returns>
        Task<string> HashPasswordAsync(string password);

        /// <summary>
        /// Validates a password against its hashed value.
        /// </summary>
        /// <param name="password">The plain-text password to validate.</param>
        /// <param name="hashedPassword">The hashed password to validate against.</param>
        /// <returns>Whether the password matches the hashed value.</returns>
        Task<bool> ValidatePasswordHashAsync(string password, string hashedPassword);

        /// <summary>
        /// Unlocks a user account.
        /// </summary>
        /// <param name="userId">The ID of the user to unlock.</param>
        /// <returns>Whether the account was successfully unlocked.</returns>
        Task<bool> UnlockAccountAsync(int userId);
    }
}
