﻿using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Services
{
    public interface ISessionService
    {
        Task InsertSession(SessionModel session);
        Task<string> RefreshSession(int userId, string oldVerificationId, string newVerificationId);
        Task LogoutSession(string token, int userId);
        Task<List<SessionModel>> GetActiveSessionsForUser(int userId);
        Task<List<SessionModel>> GetMostRecentActivity();
    }
}