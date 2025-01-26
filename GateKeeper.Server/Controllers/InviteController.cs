using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site; // Your Invite model location
using System.Data;
using GateKeeper.Server.Extension;
using MySqlConnector;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Configuration;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Inherites;

namespace GateKeeper.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InviteController : MyControllerBase
    {
        private readonly IInviteService _inviteService;
        private readonly ILogger<InviteController> _logger;
        private readonly bool _requiresInvite;

        public InviteController(
            IInviteService inviteService,
            ILogger<InviteController> logger, IConfiguration configuration)
        {
            _inviteService = inviteService;
            _logger = logger;
            _requiresInvite = configuration.GetValue<bool>("RegisterSettings:RequireInvite");
        }

        /// <summary>
        /// Sends an invite using the SendInvite method in InviteService.
        /// </summary>
        /// <param name="invite">The invite data in the request body.</param>
        /// <returns>ActionResult indicating success or failure.</returns>
        [HttpPost("SendInvite")]
        [Authorize] // Adjust role as needed
        public async Task<IActionResult> SendInvite([FromBody] Invite invite)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserIdFromClaims();
            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();

            try
            {
                // Optionally confirm that the invite.FromId is the same as the caller's userId if needed.
                // e.g. invite.FromId = userId; 

                var newInviteId = await _inviteService.SendInvite(invite);
                if (newInviteId == 0)
                {
                    // If 0 returned, assume something went wrong or template was missing
                    return BadRequest(new { error = "Failed to send invite. Template or other data might be missing." });
                }

                return Ok(new { message = "Invite sent successfully.", inviteId = newInviteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{FunctionName}('{FromId}', '{ToName}', '{ToEmail}', '{IpAddress}') Error: {ErrorMessage}",
                    FunctionName(),
                    invite.FromId,
                    invite.ToName.SanitizeForLogging(),
                    invite.ToEmail.SanitizeForLogging(),
                    userIp,
                    ex.Message
                );
                return StatusCode(500, new { error = "An error occurred while sending the invite." });
            }
            finally
            {
                _logger.LogInformation(
                    "Invite send attempted: FromId: {FromId}, IP: {IpAddress}, Device: {UserAgent}",
                    invite.FromId,
                    userIp,
                    userAgent
                );
            }
        }

        /// <summary>
        /// Retrieves invites by a given FromId (the sender's user ID).
        /// </summary>
        /// <param name="fromId">User ID of the invite sender.</param>
        /// <returns>List of invites.</returns>
        [HttpGet("from/{fromId}")]
        [Authorize] // Adjust role as needed
        public async Task<IActionResult> GetInvitesByFromId(int fromId)
        {
            var userId = GetUserIdFromClaims();
            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            try
            {
                // Optionally enforce that fromId == userId if you only want users
                // to see their own invites:
                // if (fromId != userId) return Forbid();

                var invites = await _inviteService.GetInvitesByFromId(fromId);
                return Ok(invites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{FunctionName}('{FromId}', '{IpAddress}') Error: {ErrorMessage}",
                    FunctionName(),
                    fromId,
                    userIp,
                    ex.Message
                );
                return StatusCode(500, new { error = "An error occurred while retrieving invites." });
            }
            finally
            {
                _logger.LogInformation(
                    "{FunctionName} attempted: FromId: {FromId}, IP: {IpAddress}", 
                    FunctionName(), fromId, userIp
                );
            }
        }

        /// <summary>
        /// Checks if the registration requires an invitation.
        /// </summary>
        /// <returns>Action result indicating if the registration requires an invitation.</returns>
        [HttpGet("is-invite-only")]
        [AllowAnonymous]
        public IActionResult IsInviteOnly()
        {
            try
            {
                return Ok(new { _requiresInvite });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{FunctionName} Error: {ErrorMessage}",
                    FunctionName(),
                    ex.Message
                );
                return StatusCode(500, new { error = "Error retrieving invites required." });
            }
            finally
            {
                _logger.LogInformation("{FunctionName} attempted", FunctionName());
            }
        }

        #region Private Helper Methods

        private int GetUserIdFromClaims()
        {
            return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId)
                ? userId
                : 0;
        }

        #endregion
    }
}
