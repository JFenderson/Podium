using Hangfire.Dashboard;
using Podium.Core.Constants;

namespace Podium.API.Filters
{
    public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Must be authenticated
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
                return false;

            // Must be a Director (admin-level role)
            return httpContext.User.IsInRole(Roles.Director);
        }
    }
}
