﻿using Microsoft.AspNetCore.Mvc;
using GateKeeper.Server.Interface;
using Microsoft.AspNetCore.Authorization;
using GateKeeper.Server.Models.Site;
using System.Security.Claims;
using GateKeeper.Server.Extension;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling settings-related operations such as retrieval, insertion, updating, and deletion.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        /// <summary>
        /// Constructor for the SettingsController.
        /// </summary>
        /// <param name="settingsService">Settings service dependency.</param>
        /// <param name="logger">Logger dependency.</param>
        public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all settings from the system.
        /// </summary>
        /// <returns>A list of settings.</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllSettings()
        {
            try
            {
                List<Setting> settingsToReturn;

                if (User.IsInRole("Admin"))
                {
                    // Admins see only global settings on this general listing.
                    // User-specific settings for admins (if any) could be managed elsewhere or via specific queries.
                    var globalSettings = await _settingsService.GetAllSettingsAsync(null); // Pass null to get only global settings

                    // The existing LINQ processing is designed for override logic.
                    // If we only fetch global settings (UserId will be null), this LINQ might be overly complex
                    // or could be simplified. For now, let's see its effect.
                    // All s.UserId.HasValue will be false. So it will always hit the 'else return group'.
                    // This means the grouping might still be relevant if global settings could have duplicates by name/category
                    // (which they shouldn't ideally, but the logic handles it).
                    settingsToReturn = globalSettings
                        .GroupBy(s => new { s.Name, s.Category })
                        .SelectMany(group => group) // Simpler: take all from the group as they are all global
                        .ToList();
                    // A more direct approach if only global settings are fetched and no override logic is needed:
                    // settingsToReturn = globalSettings;
                }
                else
                {
                    var userId = GetUserIdFromClaims();
                    var userSpecificAndGlobalSettings = await _settingsService.GetAllSettingsAsync(userId);
                    settingsToReturn = userSpecificAndGlobalSettings
                        .GroupBy(s => new { s.Name, s.Category })
                        .SelectMany(group =>
                        {
                            if (group.Any(s => s.UserId.HasValue))
                            {
                                return group.Where(s => s.UserId.HasValue);
                            }
                            return group;
                        })
                        .ToList();
                }
                return Ok(settingsToReturn);
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Retrieves a single setting by its Id.
        /// </summary>
        /// <param name="id">The unique identifier of the setting.</param>
        /// <returns>The requested setting or 404 if not found.</returns>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetSettingById(int id)
        {
            try
            {
                var setting = await _settingsService.GetSettingByIdAsync(id);
                if (setting == null)
                {
                    return NotFound(new { message = $"Setting with Id {id} not found." });
                }
                return Ok(setting);
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Creates a new setting.
        /// </summary>
        /// <param name="setting">A Setting object containing the details of the new setting.</param>
        /// <returns>The newly created setting.</returns>
        [HttpPost("AddUserSetting")]
        [Authorize]
        public async Task<IActionResult> AddUserSetting([FromBody] Setting setting)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetUserIdFromClaims();
                setting.UserId = userId;
                setting.UpdatedBy = userId;
                setting.CreatedBy = userId;
                var createdSetting = await _settingsService.AddSettingAsync(setting);
                return CreatedAtAction(nameof(GetSettingById), new { id = createdSetting.Id }, new { message = "Setting created successfully.", setting = createdSetting });
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Creates a new setting.
        /// </summary>
        /// <param name="setting">A Setting object containing the details of the new setting.</param>
        /// <returns>The newly created setting.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddSetting([FromBody] Setting setting)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetUserIdFromClaims();
                setting.UserId = null;
                setting.UpdatedBy = userId;
                setting.CreatedBy = userId;
                var createdSetting = await _settingsService.AddSettingAsync(setting);
                return CreatedAtAction(nameof(GetSettingById), new { id = createdSetting.Id }, new { message = "Setting created successfully.", setting = createdSetting });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new setting: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { error = "Error creating new setting" });
            }
        }

        /// <summary>
        /// Updates an existing setting.
        /// </summary>
        /// <param name="id">The Id of the setting to update.</param>
        /// <param name="setting">The updated data for the setting.</param>
        /// <returns>The updated setting or 404 if not found.</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSetting(int id, [FromBody] Setting setting)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != setting.Id)
            {
                return BadRequest(new { message = "Setting ID mismatch." });
            }

            try
            {
                var userId = GetUserIdFromClaims();
                setting.UpdatedBy = userId;
                var updatedSetting = await _settingsService.UpdateSettingAsync(setting);
                if (updatedSetting == null)
                {
                    return NotFound(new { message = $"Setting with Id {id} not found." });
                }

                return Ok(new { message = "Setting updated successfully.", setting = updatedSetting });
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Deletes a setting by its Id.
        /// </summary>
        /// <param name="id">The unique identifier of the setting to delete.</param>
        /// <returns>Status of the deletion operation.</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSetting(int id)
        {
            try
            {
                var success = await _settingsService.DeleteSettingAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Setting with Id {id} not found." });
                }

                return Ok(new { message = "Setting deleted successfully." });
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Retrieves all settings within a specific category.
        /// </summary>
        /// <param name="category">The category of settings to retrieve.</param>
        /// <returns>A list of settings within the specified category.</returns>
        [HttpGet("Category/{category}")]
        [Authorize]
        public async Task<IActionResult> GetSettingsByCategory(string category)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var settings = await _settingsService.GetSettingsByCategoryAsync(userId, category);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Searches settings based on name and/or category with pagination.
        /// </summary>
        /// <param name="name">Partial or full name to search for.</param>
        /// <param name="category">Category to filter by.</param>
        /// <param name="limit">Number of records to retrieve.</param>
        /// <param name="offset">Number of records to skip.</param>
        /// <returns>A list of matching settings.</returns>
        [HttpGet("Search")]
        [Authorize]
        public async Task<IActionResult> SearchSettings([FromQuery] string? name, [FromQuery] string? category, [FromQuery] int limit = 10, [FromQuery] int offset = 0)
        {
            try
            {
                var settings = await _settingsService.SearchSettingsAsync(name, category, limit, offset);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Adds a new setting or updates it if it already exists based on the Name.
        /// </summary>
        /// <param name="setting">A Setting object containing the details of the setting to add or update.</param>
        /// <returns>The added or updated setting.</returns>
        [HttpPost("AddOrUpdate")]
        [Authorize]
        public async Task<IActionResult> AddOrUpdateSetting([FromBody] Setting setting)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetUserIdFromClaims();
                setting.UserId = userId;
                setting.UpdatedBy = userId;
                if (setting.Id == 0) setting.CreatedBy = userId;

                var resultSetting = await _settingsService.AddOrUpdateSettingAsync(userId, setting);
                if (resultSetting == null)
                {
                    return BadRequest(new { message = "AddOrUpdate operation failed." });
                }

                return Ok(new { message = "Setting added or updated successfully.", setting = resultSetting });
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        #region private Functions

        private int GetUserIdFromClaims()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        // Removed HandleInternalError method as its functionality is now covered by GlobalExceptionHandlerMiddleware
        #endregion
    }
}
