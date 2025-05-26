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
            if (resourceFileName.Contains("..") || resourceFileName.Contains("/") || resourceFileName.Contains("\\"))
                return StatusCode(400, new { error = "Invalid path" });
            try
            {
                var entries = _resourceService.ListEntries(resourceFileName);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
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
            if (resourceFileName.Contains("..") || resourceFileName.Contains("/") || resourceFileName.Contains("\\"))
                return StatusCode(400, new { error = "Invalid path" });
            try
            {
                _resourceService.AddEntry(resourceFileName, request);
                return CreatedAtAction(nameof(GetEntries), new { resourceFileName = resourceFileName }, request);
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
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
            if (resourceFileName.Contains("..") || resourceFileName.Contains("/") || resourceFileName.Contains("\\"))
                return StatusCode(400, new { error = "Invalid path" });
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
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }
    }
}
