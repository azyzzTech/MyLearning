using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace hangfire.web.api.v1.Filters
{
    public class JobsAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}
