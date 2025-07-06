using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.Login;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Extension;

namespace GateKeeper.Server.Services.Site
{
    public interface IVerifyTokenService
    {
        Task<TokenVerificationResponse> VerifyTokenAsync(VerifyTokenRequest verificationCode);
        Task<string> GenerateTokenAsync(int userId, string verifyType);
        Task<int> RevokeTokensAsync(int userId, string verifyType, string? token = null);
        Task<int> CompleteTokensAsync(int userId, string verifyType, string? token = null);
    }

    /// <summary>
    /// Service handling verification token operations.
    /// </summary>
    public class VerifyTokenService(IVerifyTokenRepository verifyTokenRepository, ILogger<VerifyTokenService> logger, IUserService userService) : IVerifyTokenService
    {
        private readonly IVerifyTokenRepository _verifyTokenRepository = verifyTokenRepository;
        private readonly ILogger<VerifyTokenService> _logger = logger;
        private readonly IUserService _userService = userService;

        /// <inheritdoc />
        public async Task<TokenVerificationResponse> VerifyTokenAsync(VerifyTokenRequest verifyRequest)
        {
            var response = new TokenVerificationResponse
            {
                VerificationCode = verifyRequest.VerificationCode
            };

            var tokenParts = verifyRequest.VerificationCode.Split('.');
            if (tokenParts.Length != 2)
            {
                response.FailureReason = "Invalid token format";
                return response;
            }

            response.SessionId = tokenParts[0]; // This is actually the tokenId
            var providedTokenPart = tokenParts[1];

            var tokenDetails = await _verifyTokenRepository.GetTokenDetailsForVerificationAsync(response.SessionId);

            if (tokenDetails == null)
            {
                response.FailureReason = "Invalid Session Id"; // Or "Token ID not found"
                return response;
            }

            if (tokenDetails.Revoked)
            {
                response.FailureReason = "Token already revoked";
                return response;
            }

            if (tokenDetails.Complete)
            {
                response.FailureReason = "Token already completed";
                return response;
            }

            response.TokenType = tokenDetails.VerifyType;
            if (verifyRequest.TokenType != response.TokenType)
            {
                response.FailureReason = "Incorrect token type";
                return response;
            }

            var hashedProvidedToken = PasswordHelper.HashPassword(providedTokenPart, tokenDetails.RefreshSalt);

            if (tokenDetails.HashedToken != hashedProvidedToken)
            {
                response.FailureReason = "Invalid token";
                // User object is not fully populated yet to call ClearPHIAsync directly on response.User
                // We'll create the user object below, and if this check fails, it will be cleared.
            }

            // Construct the User object from tokenDetails
            // Note: The VerificationTokenDetails model now contains user fields from the SP.
            response.User = new User()
            {
                Id = tokenDetails.UserId,
                FirstName = tokenDetails.FirstName,
                LastName = tokenDetails.LastName,
                Email = tokenDetails.Email,
                Phone = tokenDetails.Phone,
                Salt = tokenDetails.UserSalt, // User's main password salt from SP
                Password = tokenDetails.UserPassword, // User's main password hash from SP
                Username = tokenDetails.Username,
                Roles = await _userService.GetRolesAsync(tokenDetails.UserId) // Still get roles separately for consistency and up-to-date info
            };

            if (tokenDetails.HashedToken != hashedProvidedToken) // Re-check here after User object is populated
            {
                // response.FailureReason is already set above
                await response.User.ClearPHIAsync(); // Now safe to call
                return response;
            }

            response.IsVerified = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<string> GenerateTokenAsync(int userId, string verifyType)
        {
            var rawVerifyToken = GenerateRawVerifyToken();
            _logger.LogInformation("Generating raw token part for User ID: {UserId}, Type: {VerifyType}", userId, verifyType);

            var salt = PasswordHelper.GenerateSalt();
            _logger.LogInformation("Generated salt for token: {Salt}", salt);

            var hashedVerifyToken = PasswordHelper.HashPassword(rawVerifyToken, salt);
            _logger.LogInformation("Generated hashed token: {HashedToken}", hashedVerifyToken.SanitizeForLogging());

            var expiryDate = DateTime.UtcNow.AddDays(7); // 7-day expiration, consider making this configurable

            // Store using repository
            var tokenId = await _verifyTokenRepository.StoreTokenAsync(userId, verifyType, hashedVerifyToken, salt, expiryDate);
            _logger.LogInformation("Stored token with ID: {TokenId} for User ID: {UserId}", tokenId, userId);

            return $"{tokenId}.{rawVerifyToken}";
        }

        /// <inheritdoc />
        public async Task<int> RevokeTokensAsync(int userId, string verifyType, string? token = null)
        {
            string? tokenId = null;
            if (!string.IsNullOrEmpty(token) && token.Contains('.'))
            {
                tokenId = token.Split('.')[0];
            }
            else if (!string.IsNullOrEmpty(token)) // If token is passed but not in expected format, it might be just the ID
            {
                _logger.LogWarning("RevokeTokensAsync received a token '{TokenValue}' not in 'tokenId.rawValue' format. Assuming it's a tokenId.", token.SanitizeForLogging());
                tokenId = token; // Or treat as an error, depending on expected usage
            }


            _logger.LogInformation("Attempting to revoke token. User ID: {UserId}, Type: {VerifyType}, Token ID: {TokenId}", userId, verifyType, tokenId ?? "All");
            var rowsAffected = await _verifyTokenRepository.RevokeTokensAsync(userId, verifyType, tokenId);
            _logger.LogInformation("{RowsAffected} token(s) revoked.", rowsAffected);
            return rowsAffected;
        }

        /// <inheritdoc />
        public async Task<int> CompleteTokensAsync(int userId, string verifyType, string? token = null)
        {
            string? tokenId = null;
            if (!string.IsNullOrEmpty(token) && token.Contains('.'))
            {
                tokenId = token.Split('.')[0];
            }
            else if (!string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("CompleteTokensAsync received a token '{TokenValue}' not in 'tokenId.rawValue' format. Assuming it's a tokenId.", token.SanitizeForLogging());
                tokenId = token;
            }

            _logger.LogInformation("Attempting to complete token. User ID: {UserId}, Type: {VerifyType}, Token ID: {TokenId}", userId, verifyType, tokenId ?? "All");
            var rowsAffected = await _verifyTokenRepository.CompleteTokensAsync(userId, verifyType, tokenId);
            _logger.LogInformation("{RowsAffected} token(s) completed.", rowsAffected);
            return rowsAffected;
        }

        #region Private Helper Methods

        /// <summary>
        /// Generates a secure raw (unhashed) verification token part.
        /// </summary>
        /// <returns>Raw token part as a string.</returns>
        private string GenerateRawVerifyToken()
        {
            var randomBytes = new byte[64]; // For a Base64 string of length 88
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        #endregion
    }
}
