using GateKeeper.Server.Models.Account;
using System;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Repository interface for managing verification tokens in the database.
    /// </summary>
    public interface IVerifyTokenRepository
    {
        /// <summary>
        /// Retrieves token details for verification.
        /// </summary>
        /// <param name="tokenId">The ID of the token (the part before the dot).</param>
        /// <returns>A <see cref="VerificationTokenDetails"/> object if found; otherwise, null.</returns>
        Task<VerificationTokenDetails?> GetTokenDetailsForVerificationAsync(string tokenId);

        /// <summary>
        /// Stores a new verification token.
        /// </summary>
        /// <param name="userId">The ID of the user associated with the token.</param>
        /// <param name="verifyType">The type of verification (e.g., "PasswordReset", "EmailVerification").</param>
        /// <param name="hashedToken">The hashed value of the token.</param>
        /// <param name="salt">The salt used to hash the token.</param>
        /// <param name="expiryDate">The expiry date of the token.</param>
        /// <returns>The generated unique ID for the stored token (e.g., a GUID string).</returns>
        Task<string> StoreTokenAsync(int userId, string verifyType, string hashedToken, string salt, DateTime expiryDate);

        /// <summary>
        /// Marks tokens as revoked.
        /// </summary>
        /// <param name="userId">The ID of the user whose tokens are to be revoked.</param>
        /// <param name="verifyType">The type of token to revoke.</param>
        /// <param name="tokenId">Optional. The specific token ID to revoke. If null, all tokens of the specified type for the user may be revoked (behavior depends on SP).</param>
        /// <returns>The number of tokens affected.</returns>
        Task<int> RevokeTokensAsync(int userId, string verifyType, string? tokenId);

        /// <summary>
        /// Marks tokens as complete.
        /// </summary>
        /// <param name="userId">The ID of the user whose tokens are to be marked complete.</param>
        /// <param name="verifyType">The type of token to complete.</param>
        /// <param name="tokenId">Optional. The specific token ID to complete. If null, all tokens of the specified type for the user may be completed (behavior depends on SP).</param>
        /// <returns>The number of tokens affected.</returns>
        Task<int> CompleteTokensAsync(int userId, string verifyType, string? tokenId);
    }
}
