using GateKeeper.Server.Models.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface ISessionRepository
    {
        Task InsertAsync(SessionModel session);
        Task<string> RefreshAsync(int userId, string oldVerificationId, string newVerificationId);
        Task LogoutByVerificationIdAsync(string verificationId);
        Task LogoutBySessionIdAsync(string sessionId);
        Task<List<SessionModel>> GetActiveByUserIdAsync(int userId);
        Task<List<SessionModel>> GetMostRecentAsync();
    }
}
