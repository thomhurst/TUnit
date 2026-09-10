using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using TUnit.OpenTelemetry;

namespace TUnit.AspNetCore;

/// <summary>
/// Stamps every incoming request's Activity with a baggage entry that identifies
/// the owning <c>TestWebApplicationFactory</c>. The companion
/// <see cref="TUnitTestCorrelationProcessor"/> uses that baggage to refuse to tag
/// activities triggered by a sibling factory's request pipeline.
/// </summary>
/// <remarks>
/// Without this filter, multiple parallel factories that each register a
/// <c>TracerProvider</c> against the process-global <c>Microsoft.AspNetCore</c>
/// <see cref="System.Diagnostics.ActivitySource"/> all observe the same Activity
/// for any one request, and any one factory's processor can stamp the others'
/// activities — which is what produced the foreign <c>tunit.test.id</c> in the
/// opt-out exporter on macOS in run 25572055061.
/// </remarks>
internal sealed class FactoryScopeStartupFilter : IStartupFilter
{
    private readonly CorrelationScope _scope;

    public FactoryScopeStartupFilter(CorrelationScope scope)
    {
        _scope = scope;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            app.Use(async (context, requestNext) =>
            {
                var activity = Activity.Current;
                // Setting baggage on Activity.Current rather than the activity returned by
                // an OpenTelemetry enricher means the value is available to every processor
                // observing this activity, including those from sibling factories. They use
                // it to recognise that the activity is not theirs.
                activity?.AddBaggage(CorrelationScope.FactoryIdBaggageKey, _scope.FactoryId);
                await requestNext();
            });
            next(app);
        };
}
