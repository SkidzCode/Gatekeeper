using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Site;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Services
{
    public class InviteService : IInviteService
    {
        private readonly IInviteRepository _inviteRepository;
        private readonly ILogger<InviteService> _logger;
        private readonly IVerifyTokenService _verificationService;
        private readonly INotificationService _notificationService;
        private readonly INotificationTemplateService _notificationTemplateService;

        public InviteService(
            IInviteRepository inviteRepository,
            ILogger<InviteService> logger,
            IVerifyTokenService veryTokenService,
            INotificationService notificationService,
            INotificationTemplateService notificationTemplateService)
        {
            _inviteRepository = inviteRepository;
            _logger = logger;
            _verificationService = veryTokenService;
            _notificationService = notificationService;
            _notificationTemplateService = notificationTemplateService;
        }

        public async Task<int> SendInvite(Invite invite)
        {
            var template = await _notificationTemplateService.GetNotificationTemplateByNameAsync("InviteUserTemplate");
            if (template == null)
            {
                _logger.LogError("Invite template not found");
                return 0;
            }

            var response = await _notificationService.InsertNotificationAsync(new Notification()
            {
                Channel = "Email",
                Message = template.Body,
                Subject = template.Subject,
                RecipientId = 0,
                TokenType = template.TokenType,
                URL = invite.Website,
                FromId = invite.FromId,
                ToEmail = invite.ToEmail,
                ToName = invite.ToName
            });

            invite.NotificationId = response.NotificationId;
            invite.VerificationId = response.VerificationId;
            int inviteId = await _inviteRepository.InsertInviteAsync(invite);

            return inviteId;
        }

        public async Task<int> InsertInvite(Invite invite)
        {
            return await _inviteRepository.InsertInviteAsync(invite);
        }

        public async Task<List<Invite>> GetInvitesByFromId(int fromId)
        {
            return await _inviteRepository.GetInvitesByFromIdAsync(fromId);
        }
    }
}
