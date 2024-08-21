using Microsoft.AspNetCore.Mvc;

namespace MarketInsights.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarketController : ControllerBase
    {
        private readonly AppState appState;

        public MarketController(AppState appState)
        {
            this.appState = appState;
        }

        /// <summary>
        /// Gets the most recent market overview.
        /// </summary>
        [HttpGet]
        public MarketOverview Get()
        {
            return appState.MarketOverview;
        }
    }
}
