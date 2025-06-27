using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models.Account.Login; // Required for VerifyTokenRequest

namespace GateKeeper.Server.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ILogger<SessionService> _logger;
        private readonly IVerifyTokenService _verifyTokenService;

        public SessionService(
            ISessionRepository sessionRepository,
            ILogger<SessionService> logger,
            IVerifyTokenService verifyTokenService)
        {
            _sessionRepository = sessionRepository;
            _logger = logger;
            _verifyTokenService = verifyTokenService;
        }

        public async Task InsertSession(SessionModel session)
        {
            _logger.LogInformation("Inserting session for User ID: {UserId}, Verification ID: {VerificationId}", session.UserId, session.VerificationId);
            await _sessionRepository.InsertAsync(session);
        }

        public async Task<string> RefreshSession(int userId, string oldVerificationId, string newVerificationId)
        {
            _logger.LogInformation("Refreshing session for User ID: {UserId}, Old Verification ID: {OldVerificationId}", userId, oldVerificationId);
            return await _sessionRepository.RefreshAsync(userId, oldVerificationId, newVerificationId);
        }

        public async Task LogoutToken(string token, int userId)
        {
            _logger.LogInformation("Attempting to logout token for User ID: {UserId}", userId);
            var response = await _verifyTokenService.VerifyTokenAsync(new VerifyTokenRequest()
            {
                TokenType = "Refresh",
                VerificationCode = token
            });

            if (!response.IsVerified || userId != response.User.Id)
            {
                _logger.LogWarning("Token verification failed or user mismatch during logout for User ID: {UserId}. Verification Status: {IsVerified}, Token User ID: {TokenUserId}", userId, response.IsVerified, response.User?.Id);
                return;
            }

            string verificationId = token.Split('.')[0];
            _logger.LogInformation("Logging out session by Verification ID: {VerificationId} for User ID: {UserId}", verificationId, userId);
            await _sessionRepository.LogoutByVerificationIdAsync(verificationId);
        }

        public async Task LogoutSession(string sessionId, int userId)
        {
            // userId parameter is not directly used by the repository method but good for logging context
            _logger.LogInformation("Logging out session by Session ID: {SessionId} for User ID: {UserId}", sessionId, userId);
            await _sessionRepository.LogoutBySessionIdAsync(sessionId);
        }

        public async Task<List<SessionModel>> GetActiveSessionsForUser(int userId)
        {
            _logger.LogDebug("Fetching active sessions for User ID: {UserId}", userId);
            return await _sessionRepository.GetActiveByUserIdAsync(userId);
        }

        public async Task<List<SessionModel>> GetMostRecentActivity()
        {
            _logger.LogDebug("Fetching most recent session activity.");
            return await _sessionRepository.GetMostRecentAsync();
        }
    }
}
