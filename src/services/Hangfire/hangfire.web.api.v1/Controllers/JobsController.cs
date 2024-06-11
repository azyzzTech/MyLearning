using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace hangfire.web.api.v1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : Controller
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public JobsController(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpPost("enqueue")]
        public IActionResult EnqueueJob()
        {
            _backgroundJobClient.Enqueue(() => Console.WriteLine("Enqueue Job Runned!"));
            return Ok("Job Has Been Added To Queue");
        }

        [HttpPost("schedule")]
        public IActionResult ScheduleJob()
        {
            _backgroundJobClient.Schedule(() => Console.WriteLine("Scheduled Job Runned!"), TimeSpan.FromMinutes(1));
            return Ok("Background Job Has Been Scheduled To Run After 1 Minute");
        }
    }
}
