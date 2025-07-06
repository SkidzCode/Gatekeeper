using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Services.Site
{
    public class SessionService(
        ISessionRepository sessionRepository,
        ILogger<SessionService> logger,
        IVerifyTokenService verifyTokenService)
        : ISessionService
    {
        public async Task InsertSession(SessionModel session)
        {
            logger.LogInformation("Inserting session for User ID: {UserId}, Verification ID: {VerificationId}", session.UserId, session.VerificationId);
            await sessionRepository.InsertAsync(session);
        }

        public async Task<string> RefreshSession(int userId, string oldVerificationId, string newVerificationId)
        {
            logger.LogInformation("Refreshing session for User ID: {UserId}, Old Verification ID: {OldVerificationId}", userId, oldVerificationId);
            return await sessionRepository.RefreshAsync(userId, oldVerificationId, newVerificationId);
        }

        public async Task LogoutToken(string token, int userId)
        {
            logger.LogInformation("Attempting to logout token for User ID: {UserId}", userId);
            var response = await verifyTokenService.VerifyTokenAsync(new VerifyTokenRequest()
            {
                TokenType = "Refresh",
                VerificationCode = token
            });

            if (!response.IsVerified || (response.User != null && userId != response.User.Id))
            {
                logger.LogWarning("Token verification failed or user mismatch during logout for User ID: {UserId}. Verification Status: {IsVerified}, Token User ID: {TokenUserId}", userId, response.IsVerified, response.User?.Id);
                return;
            }

            string verificationId = token.Split('.')[0];
            logger.LogInformation("Logging out session by Verification ID: {VerificationId} for User ID: {UserId}", verificationId, userId);
            await sessionRepository.LogoutByVerificationIdAsync(verificationId);
        }

        public async Task LogoutSession(string sessionId, int userId)
        {
            // userId parameter is not directly used by the repository method but good for logging context
            logger.LogInformation("Logging out session by Session ID: {SessionId} for User ID: {UserId}", sessionId, userId);
            await sessionRepository.LogoutBySessionIdAsync(sessionId);
        }

        public async Task<List<SessionModel>> GetActiveSessionsForUser(int userId)
        {
            logger.LogDebug("Fetching active sessions for User ID: {UserId}", userId);
            return await sessionRepository.GetActiveByUserIdAsync(userId);
        }

        public async Task<List<SessionModel>> GetMostRecentActivity()
        {
            logger.LogDebug("Fetching most recent session activity.");
            return await sessionRepository.GetMostRecentAsync();
        }
    }
}
