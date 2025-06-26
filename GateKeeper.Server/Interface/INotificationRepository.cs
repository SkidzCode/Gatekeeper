using GateKeeper.Server.Models.Account.Notifications;
using GateKeeper.Server.Models.Site;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetAllAsync();
        Task<List<Notification>> GetByRecipientIdAsync(int recipientId);
        Task<List<Notification>> GetNotSentAsync(DateTime currentTime);
        Task<int> InsertAsync(Notification notification); // Returns the ID of the inserted notification
        Task UpdateAsync(Notification notification);
    }
}
