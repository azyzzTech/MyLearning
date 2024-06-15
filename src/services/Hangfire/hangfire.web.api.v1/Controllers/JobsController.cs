using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace hangfire.web.api.v1.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public JobsController(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        // Fire-and-Forget Jobs
        [HttpPost("enqueue")]
        public IActionResult EnqueueJob()
        {
            _backgroundJobClient.Enqueue(() => Console.WriteLine("Fire-and-Forget Job Executed!"));
            return Ok("Job has been added to the queue.");
        }

        // Delayed Jobs
        [HttpPost("schedule")]
        public IActionResult ScheduleJob()
        {
            _backgroundJobClient.Schedule(() => Console.WriteLine("Scheduled Job Executed!"), TimeSpan.FromMinutes(1));
            return Ok("Job has been scheduled to run after 1 minute.");
        }

        // Recurring Jobs
        [HttpPost("recurring/addOrUpdate")]
        public IActionResult AddOrUpdateRecurringJob()
        {
            RecurringJob.AddOrUpdate("recurringJob", () => Console.WriteLine("Recurring Job Executed!"), Cron.Daily);
            return Ok("Recurring job has been added or updated.");
        }

        // Continuation Jobs
        [HttpPost("continueWith")]
        public IActionResult ContinueWithJob()
        {
            var jobId = _backgroundJobClient.Enqueue(() => Console.WriteLine("Initial Job Executed!"));
            _backgroundJobClient.ContinueJobWith(jobId, () => Console.WriteLine("Continuation Job Executed!"));
            return Ok("Continuation job has been executed.");
        }

        // Remove Recurring Job If Exists
        [HttpPost("recurring/removeIfExists")]
        public IActionResult RemoveRecurringJobIfExists()
        {
            RecurringJob.RemoveIfExists("recurringJob");
            return Ok("Recurring job has been removed if it existed.");
        }

        // Delete Job
        [HttpPost("delete")]
        public IActionResult DeleteJob([FromBody] string jobId)
        {
            BackgroundJob.Delete(jobId);
            return Ok("Job has been deleted.");
        }

        // Requeue Job
        [HttpPost("requeue")]
        public IActionResult RequeueJob([FromBody] string jobId)
        {
            BackgroundJob.Requeue(jobId);
            return Ok("Job has been requeued.");
        }
    }
}
