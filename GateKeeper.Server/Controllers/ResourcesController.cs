using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GateKeeper.Server.Models.Resources;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using Microsoft.AspNetCore.Authorization;

namespace GateKeeper.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResourcesController : ControllerBase
    {
        private readonly IResourceService _resourceService;
        private readonly ILogger<ResourcesController> _logger;

        public ResourcesController(IResourceService resourceService, ILogger<ResourcesController> logger)
        {
            _resourceService = resourceService;
            _logger = logger;
        }

        /// <summary>
        /// Get all entries from a given resource file.
        /// </summary>
        /// <param name="resourceFileName">The name of the resource file without the extension.</param>
        /// <returns>List of resource entries</returns>
        [HttpGet("{resourceFileName}")]
        [Authorize(Roles = "Admin")]
        public ActionResult<List<ResourceEntry>> GetEntries([FromRoute] string resourceFileName)
        {
            try
            {
                var entries = _resourceService.ListEntries(resourceFileName);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format("An unexpected error occurred while retrieving entries: {0}", ex.Message);
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Add a new entry to the resource file.
        /// </summary>
        /// <param name="resourceFileName">Resource file name.</param>
        /// <param name="request">Entry details.</param>
        [HttpPost("{resourceFileName}")]
        [Authorize(Roles = "Admin")]
        public IActionResult AddEntry([FromRoute] string resourceFileName, [FromBody] AddResourceEntryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(request);
            }

            try
            {
                _resourceService.AddEntry(resourceFileName, request);
                return CreatedAtAction(nameof(GetEntries), new { resourceFileName = resourceFileName }, request);
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format("An unexpected error occurred while adding a new entry: {0}", ex.Message);
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Update an existing entry in the resource file.
        /// </summary>
        /// <param name="resourceFileName">Resource file name.</param>
        /// <param name="key">Key of the resource entry to update.</param>
        /// <param name="request">Updated values.</param>
        [HttpPut("{resourceFileName}/{key}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateEntry([FromRoute] string resourceFileName, [FromRoute] string key, [FromBody] UpdateResourceEntryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(request);
            }

            try
            {
                _resourceService.UpdateEntry(resourceFileName, key, request);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                var errorMessage = string.Format("The specified key could not be found: {0}", ex.Message);
                _logger.LogWarning(ex, errorMessage);
                return NotFound(new { error = errorMessage });
            }
            catch (FileNotFoundException ex)
            {
                var errorMessage = string.Format("The specified resource file does not exist: {0}", ex.Message);
                _logger.LogWarning(ex, errorMessage);
                return NotFound(new { error = errorMessage });
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format("An unexpected error occurred while updating the entry: {0}", ex.Message);
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}
