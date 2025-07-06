using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Site;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Services.Site
{
    public class InviteService(
        IInviteRepository inviteRepository,
        ILogger<InviteService> logger,
        INotificationService notificationService,
        INotificationTemplateService notificationTemplateService)
        : IInviteService
    {
        public async Task<int> SendInvite(Invite invite)
        {
            var template = await notificationTemplateService.GetNotificationTemplateByNameAsync("InviteUserTemplate");
            if (template == null)
            {
                logger.LogError("Invite template not found");
                return 0;
            }

            var response = await notificationService.InsertNotificationAsync(new Notification()
            {
                Channel = "Email",
                Message = template.Body,
                Subject = template.Subject,
                RecipientId = 0,
                TokenType = template.TokenType ?? "",
                URL = invite.Website,
                FromId = invite.FromId,
                ToEmail = invite.ToEmail ?? "",
                ToName = invite.ToName ?? ""
            });

            invite.NotificationId = response.NotificationId;
            invite.VerificationId = response.VerificationId;
            int inviteId = await inviteRepository.InsertInviteAsync(invite);

            return inviteId;
        }

        public async Task<int> InsertInvite(Invite invite)
        {
            return await inviteRepository.InsertInviteAsync(invite);
        }

        public async Task<List<Invite>> GetInvitesByFromId(int fromId)
        {
            return await inviteRepository.GetInvitesByFromIdAsync(fromId);
        }
    }
}
