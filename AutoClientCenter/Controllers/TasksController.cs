using Microsoft.AspNetCore.Mvc;

namespace AutoClientCenter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService taskService;
        private static readonly object processLock = new object();

        public TasksController(ITaskService taskService)
        {
            this.taskService = taskService;
        }

        [HttpGet]
        public AcTasks Get()
        {
            return taskService.GetTasks();
        }

        [HttpPost("Results")]
        public void Post([FromBody] AcTaskStep[] taskSteps)
        {
            Task.Run(() =>
            {
                lock (processLock)
                {
                    taskService.ProcessResults(taskSteps);
                }
            });
        }
    }
}
