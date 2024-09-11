using Microsoft.AspNetCore.Mvc;

namespace AutoClientCenter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ITaskService taskService;

        public ConfigController(ITaskService taskService)
        {
            this.taskService = taskService;
        }

        [HttpGet("Stats")]
        public AcStats Get()
        {
            return taskService.GetStats();
        }

        [HttpPost("Set")]
        public void Post([FromBody] AcTasks tasks)
        {
            taskService.SetConfig(tasks);
        }
    }
}
