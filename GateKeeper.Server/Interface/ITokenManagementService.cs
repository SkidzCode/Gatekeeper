using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for token management-related operations.
    /// </summary>
    public interface ITokenManagementService
    {
        /// <summary>
        /// Generates a new access token and refresh token for a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to validate.</param>
        /// <returns>A tuple containing success status, new access token, and new refresh token.</returns>
        Task<(bool isSuccessful, string accessToken, string refreshToken)> RefreshAccessTokenAsync(string refreshToken);

        /// <summary>
        /// Revokes a specific token or all tokens associated with a user.
        /// </summary>
        /// <param name="token">The specific token to revoke, or null to revoke all tokens for the user.</param>
        /// <param name="userId">The ID of the user to revoke tokens for.</param>
        /// <returns>The number of tokens revoked.</returns>
        Task<int> RevokeTokenAsync(string? token = null, int userId = 0);

        /// <summary>
        /// Validates if a token is active and not revoked.
        /// </summary>
        /// <param name="token">The token to validate.</param>
        /// <returns>Whether the token is valid.</returns>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Adds a token to the revocation list.
        /// </summary>
        /// <param name="token">The token to add to the revocation list.</param>
        /// <returns>Whether the token was successfully added to the revocation list.</returns>
        Task<bool> AddToRevocationListAsync(string token);

        /// <summary>
        /// Checks if a token exists in the revocation list.
        /// </summary>
        /// <param name="token">The token to check.</param>
        /// <returns>Whether the token is in the revocation list.</returns>
        Task<bool> IsTokenRevokedAsync(string token);

        /// <summary>
        /// Invalidates all tokens for a user after a password reset.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The number of tokens invalidated.</returns>
        Task<int> InvalidateTokensAfterPasswordResetAsync(int userId);

        /// <summary>
        /// Lists all active tokens for a user.
        /// </summary>
        /// <param name="userId">The ID of the user to list tokens for.</param>
        /// <returns>A list of active tokens associated with the user.</returns>
        Task<IEnumerable<string>> ListActiveTokensAsync(int userId);

        /// <summary>
        /// Generates an access token and refresh token for a user.
        /// </summary>
        /// <param name="userId">The ID of the user to generate tokens for.</param>
        /// <returns>A tuple containing the access token and refresh token.</returns>
        Task<(string accessToken, string refreshToken)> GenerateTokensAsync(int userId);

        /// <summary>
        /// Checks the expiration status of a token.
        /// </summary>
        /// <param name="token">The token to check.</param>
        /// <returns>Whether the token is expired.</returns>
        Task<bool> IsTokenExpiredAsync(string token);
    }
}
