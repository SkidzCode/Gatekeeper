using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GateKeeper.Server.Services;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling service-related operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<ServicesController> _logger;

        /// <summary>
        /// Constructor for the ServicesController.
        /// </summary>
        /// <param name="sessionService">Session service dependency.</param>
        /// <param name="logger">Logger dependency.</param>
        public ServicesController(ISessionService sessionService, ILogger<ServicesController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves active sessions for the authenticated user.
        /// </summary>
        /// <returns>List of active sessions.</returns>
        [HttpGet("sessions/active")]
        [Authorize]
        public async Task<IActionResult> GetActiveSessions()
        {
            int userId = GetUserIdFromClaims();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            try
            {
                List<SessionModel> sessions = await _sessionService.GetActiveSessionsForUser(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred while retrieving active sessions.";
                _logger.LogError(ex, "Error retrieving active sessions for UserId: {UserId}, IP: {IpAddress}", userId, userIp);
                return StatusCode(500, new { error = errorMessage });
            }
            finally
            {
                _logger.LogInformation("Active sessions retrieval attempted for UserId: {UserId}, IP: {IpAddress}", userId, userIp);
            }
        }

        /// <summary>
        /// Retrieves the most recent activity sessions.
        /// </summary>
        /// <returns>List of recent sessions.</returns>
        [HttpGet("sessions/recent")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMostRecentActivity()
        {
            int adminUserId = GetUserIdFromClaims();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            try
            {
                List<SessionModel> sessions = await _sessionService.GetMostRecentActivity();
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred while retrieving recent activity sessions.";
                _logger.LogError(ex, "Error retrieving recent sessions for AdminUserId: {AdminUserId}, IP: {IpAddress}", adminUserId, userIp);
                return StatusCode(500, new { error = errorMessage });
            }
            finally
            {
                _logger.LogInformation("Recent sessions retrieval attempted by AdminUserId: {AdminUserId}, IP: {IpAddress}", adminUserId, userIp);
            }
        }

        #region Private Functions

        private int GetUserIdFromClaims()
        {
            return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId) ? userId : 0;
        }

        #endregion
    }
}
