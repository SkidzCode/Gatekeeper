﻿using System.Threading.Tasks;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.Login;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Models.Site;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for user authentication-related operations.
    /// </summary>
    public interface IUserAuthenticationService
    {
        /// <summary>
        /// Validates user credentials and generates JWT access and refresh tokens.
        /// </summary>
        /// <param name="userLogin">The user's login credentials.</param>
        /// <returns>A tuple containing success status, access token, refresh token, and user details.</returns>
        Task<LoginResponse> LoginAsync(UserLoginRequest userLogin, string ipAddress, string userAgent);

        /// <summary>
        /// Logs out a user by revoking specific or all active tokens.
        /// </summary>
        /// <param name="token">The specific token to revoke, or null to revoke all tokens for the user.</param>
        /// <param name="userId">The ID of the user to log out.</param>
        /// <returns>The number of tokens revoked.</returns>
        Task<int> LogoutAsync(string? token = null, int userId = 0);

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="user">User details for registration.</param>
        /// <returns>A Task representing the registration process.</returns>
        Task<RegistrationResponse> RegisterUserAsync(RegisterRequest user);

        /// <summary>
        /// Verifies a user's email or phone number for account activation.
        /// </summary>
        /// <param name="verificationCode">The code sent to the user's email or phone.</param>
        /// <returns>Whether the verification was successful.</returns>
        Task<TokenVerificationResponse> VerifyNewUser(string verificationCode);

        /// <summary>
        /// Refreshes JWT access and refresh tokens using a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to validate and use for generating new tokens.</param>
        /// <returns>A tuple containing success status, new access token, and new refresh token.</returns>
        Task<LoginResponse> RefreshTokensAsync(string refreshToken);

        /// <summary>
        /// Initiates a password reset process by sending a reset link or security challenge.
        /// </summary>
        /// <param name="emailOrUsername">The email or username of the user requesting a password reset.</param>
        /// <returns>A Task representing the initiation of the password reset process.</returns>
        Task InitiatePasswordResetAsync(User user, InitiatePasswordResetRequest initiateRequest);

        /// <summary>
        /// Resets the user's password using a provided reset token or security answers.
        /// </summary>
        /// <param name="resetRequest">The reset request containing the token and new password.</param>
        /// <returns>Whether the password reset was successful.</returns>
        Task<TokenVerificationResponse> ResetPasswordAsync(PasswordResetRequest resetRequest);

        /// <summary>
        /// Validates the strength of a given password.
        /// </summary>
        /// <param name="password">The password to validate.</param>
        /// <returns>Whether the password meets strength requirements.</returns>
        Task<bool> ValidatePasswordStrengthAsync(string password);

        /// <summary>
        /// Logs out the user from specific or all devices.
        /// </summary>
        /// <param name="userId">The ID of the user logging out.</param>
        /// <param name="sessionId">The session ID to log out, or null for all sessions.</param>
        /// <returns>Whether the logout was successful.</returns>
        Task<bool> LogoutFromDeviceAsync(int userId, string? sessionId = null);

        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);

        
    }
}
