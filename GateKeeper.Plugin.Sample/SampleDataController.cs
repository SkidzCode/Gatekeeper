using Microsoft.AspNetCore.Mvc;

namespace GateKeeper.Plugin.Sample
{
    [ApiController]
    [Route("api/plugins/sample")]
    public class SampleDataController : ControllerBase
    {
        [HttpGet("data")]
        public IActionResult GetData()
        {
            return Ok(new { Message = "Hello from Sample Plugin API!" });
        }
    }
}
