using System.ComponentModel;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Interface;

public interface IInviteService
{
    Task<int> SendInvite(Invite invite);
    Task<int> InsertInvite(Invite invite);
    Task<List<Invite>> GetInvitesByFromId(int fromId);
}
