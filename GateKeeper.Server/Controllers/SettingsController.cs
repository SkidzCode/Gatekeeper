﻿using Microsoft.AspNetCore.Mvc;
using GateKeeper.Server.Interface;
using Microsoft.AspNetCore.Authorization;
using GateKeeper.Server.Models.Site;

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
                var settings = await _settingsService.GetAllSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error occurred while fetching all settings: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
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
                var errorMessage = $"Error retrieving setting with Id {id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
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
                var createdSetting = await _settingsService.AddSettingAsync(setting);
                return CreatedAtAction(nameof(GetSettingById), new { id = createdSetting.Id }, new { message = "Setting created successfully.", setting = createdSetting });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error creating new setting: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
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
                var updatedSetting = await _settingsService.UpdateSettingAsync(setting);
                if (updatedSetting == null)
                {
                    return NotFound(new { message = $"Setting with Id {id} not found." });
                }

                return Ok(new { message = "Setting updated successfully.", setting = updatedSetting });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error updating setting with Id {id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
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
                var errorMessage = $"Error deleting setting with Id {id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
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
                var settings = await _settingsService.GetSettingsByCategoryAsync(category);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error retrieving settings in category '{category}': {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
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
                var errorMessage = $"Error searching settings with Name='{name}' and Category='{category}': {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Adds a new setting or updates it if it already exists based on the Name.
        /// </summary>
        /// <param name="setting">A Setting object containing the details of the setting to add or update.</param>
        /// <returns>The added or updated setting.</returns>
        [HttpPost("AddOrUpdate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddOrUpdateSetting([FromBody] Setting setting)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var resultSetting = await _settingsService.AddOrUpdateSettingAsync(setting);
                if (resultSetting == null)
                {
                    return BadRequest(new { message = "AddOrUpdate operation failed." });
                }

                return Ok(new { message = "Setting added or updated successfully.", setting = resultSetting });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error in AddOrUpdate operation for setting '{setting.Name}': {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}