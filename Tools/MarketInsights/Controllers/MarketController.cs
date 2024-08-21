using Microsoft.AspNetCore.Mvc;

namespace MarketInsights.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarketController : ControllerBase
    {
        /// <summary>
        /// Gets the most recent market overview.
        /// </summary>
        [HttpGet]
        public MarketOverview Get()
        {
            return new MarketOverview();
        }
    }
}
