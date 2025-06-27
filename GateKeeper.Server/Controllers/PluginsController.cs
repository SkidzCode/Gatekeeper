using GateKeeper.Plugin.Abstractions;
using GateKeeper.Server.Models.Plugins;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace GateKeeper.Server.Controllers
{
    [ApiController]
    [Route("api/plugins")]
    public class PluginsController : ControllerBase
    {
        private readonly IReadOnlyList<IPlugin> _plugins;

        public PluginsController(IReadOnlyList<IPlugin> plugins)
        {
            _plugins = plugins;
        }

        [HttpGet("manifests")]
        public ActionResult<IEnumerable<AngularPluginInfo>> GetPluginManifests()
        {
            var pluginInfos = _plugins.Select(plugin => new AngularPluginInfo
            {
                Name = plugin.Name,
                Version = plugin.Version,
                Description = plugin.Description
            }).ToList();

            return Ok(pluginInfos);
        }
    }
}
