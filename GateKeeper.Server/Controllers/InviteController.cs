using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site; // Your Invite model location
using System.Data;
using GateKeeper.Server.Extension;
using MySqlConnector;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InviteController : ControllerBase
    {
        private readonly IInviteService _inviteService;
        private readonly ILogger<InviteController> _logger;

        public InviteController(
            IInviteService inviteService,
            ILogger<InviteController> logger)
        {
            _inviteService = inviteService;
            _logger = logger;
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
                var errorMessage = "An error occurred while sending the invite.";
                _logger.LogError(ex,
                    "Error sending invite: FromId: {FromId}, IP: {IpAddress}, Device: {UserAgent}",
                    invite.FromId,
                    userIp,
                    userAgent
                );
                return StatusCode(500, new { error = errorMessage });
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
                var errorMessage = "An error occurred while retrieving invites.";
                _logger.LogError(ex,
                    "Error retrieving invites: FromId: {FromId}, IP: {IpAddress}",
                    fromId,
                    userIp
                );
                return StatusCode(500, new { error = errorMessage });
            }
            finally
            {
                _logger.LogInformation(
                    "Invite retrieval attempted: FromId: {FromId}, IP: {IpAddress}",
                    fromId,
                    userIp
                );
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
