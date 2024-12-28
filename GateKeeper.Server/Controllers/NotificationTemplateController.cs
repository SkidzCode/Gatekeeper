using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using GateKeeper.Server.Services;
using GateKeeper.Server.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GateKeeper.Server.Models.Site;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling notification template-related operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationTemplateController : ControllerBase
    {
        private readonly INotificationTemplateService _notificationTemplateService;
        private readonly ILogger<NotificationTemplateController> _logger;

        /// <summary>
        /// Constructor for the NotificationTemplateController.
        /// </summary>
        /// <param name="notificationTemplateService">Service for notification templates.</param>
        /// <param name="logger">Logger dependency.</param>
        public NotificationTemplateController(
            INotificationTemplateService notificationTemplateService,
            ILogger<NotificationTemplateController> logger)
        {
            _notificationTemplateService = notificationTemplateService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all notification templates from the system.
        /// </summary>
        /// <returns>A list of notification templates.</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllNotificationTemplates()
        {
            try
            {
                var templates = await _notificationTemplateService.GetAllNotificationTemplatesAsync();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error occurred while fetching all notification templates: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Retrieves a single notification template by ID.
        /// </summary>
        /// <param name="id">The unique identifier of the notification template.</param>
        /// <returns>The requested notification template or 404 if not found.</returns>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetNotificationTemplateById(int id)
        {
            try
            {
                var template = await _notificationTemplateService.GetNotificationTemplateByIdAsync(id);
                if (template == null)
                {
                    return NotFound(new { message = $"Notification Template with ID {id} not found." });
                }
                return Ok(template);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error retrieving notification template with ID {id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Creates a new notification template.
        /// </summary>
        /// <param name="template">A NotificationTemplate object containing the new template details.</param>
        /// <returns>The newly created template ID.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNotificationTemplate([FromBody] NotificationTemplate template)
        {
            // Basic validation example
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var newTemplateId = await _notificationTemplateService.InsertNotificationTemplateAsync(template);
                if (newTemplateId > 0)
                {
                    return Ok(new
                    {
                        message = "Notification Template created successfully.",
                        templateId = newTemplateId
                    });
                }
                else
                {
                    return StatusCode(500, new { error = "Failed to create notification template." });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error creating new notification template: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Updates an existing notification template.
        /// </summary>
        /// <param name="id">The ID of the notification template to update.</param>
        /// <param name="template">A NotificationTemplate object with updated data.</param>
        /// <returns>A success message or an error code.</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateNotificationTemplate(int id, [FromBody] NotificationTemplate template)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Ensure the template object's ID matches the route
                template.TemplateId = id;

                await _notificationTemplateService.UpdateNotificationTemplateAsync(template);

                return Ok(new
                {
                    message = $"Notification Template with ID {id} updated successfully."
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error updating notification template with ID {id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Deletes an existing notification template by its ID.
        /// </summary>
        /// <param name="id">The ID of the notification template to delete.</param>
        /// <returns>A success message or an error code.</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteNotificationTemplate(int id)
        {
            try
            {
                await _notificationTemplateService.DeleteNotificationTemplateAsync(id);
                return Ok(new { message = $"Notification Template with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error deleting notification template with ID {id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}
