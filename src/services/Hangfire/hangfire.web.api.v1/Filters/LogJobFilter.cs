using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;

namespace hangfire.web.api.v1.Filters
{
    public class LogJobFilter : IClientFilter, IServerFilter
    {
        //bool IJobFilter.AllowMultiple => false;

        //int IJobFilter.Order => 0;

        //public void OnPerforming(PerformContext performContext)
        //{
        //    Console.WriteLine("Job {filterContext.BackgroundJob.Id} is starting...");
        //}

        //public void OnPerformed(PerformContext performContext)
        //{
        //    Console.WriteLine("Job {filterContext.BackgroundJob.Id} has finished.");
        //}

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
