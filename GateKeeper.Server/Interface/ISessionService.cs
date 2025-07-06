using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Interface
{
    public interface ISessionService
    {
        Task InsertSession(SessionModel session);
        Task<string> RefreshSession(int userId, string oldVerificationId, string newVerificationId);
        Task LogoutToken(string token, int userId);
        Task LogoutSession(string? token, int userId);
        Task<List<SessionModel>> GetActiveSessionsForUser(int userId);
        Task<List<SessionModel>> GetMostRecentActivity();
    }
}