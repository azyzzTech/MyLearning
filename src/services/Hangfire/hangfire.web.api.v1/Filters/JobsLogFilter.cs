using Hangfire.Client;
using Hangfire.Server;

namespace hangfire.web.api.v1.Filters
{
    public class JobsLogFilter : IClientFilter, IServerFilter
    {
        public void OnCreated(CreatedContext context)
        {
            Console.WriteLine("Job {filterContext.BackgroundJob.Id} is Created.");
        }

        public void OnCreating(CreatingContext context)
        {
            Console.WriteLine("Job {filterContext.BackgroundJob.Id} is Creating..");
        }

        public void OnPerformed(PerformedContext context)
        {
            Console.WriteLine("Job {filterContext.BackgroundJob.Id} on Performed.");
        }

        public void OnPerforming(PerformingContext context)
        {
            Console.WriteLine("Job {filterContext.BackgroundJob.Id} on Performing..");
        }
    }
}
