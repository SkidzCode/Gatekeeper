using GateKeeper.Server.Models.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface IInviteRepository
    {
        Task<int> InsertInviteAsync(Invite invite);
        Task<List<Invite>> GetInvitesByFromIdAsync(int fromId);
    }
}
