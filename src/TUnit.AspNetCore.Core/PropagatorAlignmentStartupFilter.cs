using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using TUnit.Core;

namespace TUnit.AspNetCore;

/// <summary>
/// Runs <see cref="PropagatorAlignment.AlignIfDefault"/> after SUT startup code has
/// executed. Needed because user <c>Program.cs</c>/<c>Startup.cs</c> can call
/// <c>Sdk.SetDefaultTextMapPropagator(...)</c> (or otherwise reset
/// <see cref="System.Diagnostics.DistributedContextPropagator.Current"/>) during host
/// build; <see cref="IStartupFilter"/> is invoked when the pipeline is constructed,
/// which is after all service registration and startup assignments, so alignment wins.
/// </summary>
internal sealed class PropagatorAlignmentStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            PropagatorAlignment.AlignIfDefault();
            next(app);
        };
}
