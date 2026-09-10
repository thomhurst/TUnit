using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// A startup filter that hooks into the ASP.NET Core startup pipeline to track execution order.
/// Implements IStartupFilter to wrap the configure pipeline without replacing it.
/// </summary>
public class StartupFilter(TestsBase testsBase) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // Track when the startup pipeline is being configured
            if (testsBase.StartupCalledAt == null)
            {
                testsBase.StartupCalledOrder = testsBase.GetNextOrder();
                testsBase.StartupCalledAt = DateTime.UtcNow;
            }

            // Continue with the rest of the pipeline (important: don't skip this!)
            next(app);
        };
    }
}
