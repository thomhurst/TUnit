using System.ComponentModel;

namespace TUnit.OpenTelemetry;

/// <summary>
/// Identifies the factory that owns a TracerProvider, so the per-factory
/// <see cref="TUnitTestCorrelationProcessor"/> can refuse to stamp activities triggered by
/// a sibling factory's request pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Several <c>WebApplicationFactory</c> instances commonly run in parallel within one
/// process. Each calls <c>AddAspNetCoreInstrumentation()</c>, which subscribes the
/// factory's <c>TracerProvider</c> to the process-global <c>Microsoft.AspNetCore</c>
/// <see cref="System.Diagnostics.ActivitySource"/>. A single Activity created for an
/// HTTP request is therefore observed by every subscribed processor — and any tag a
/// processor writes is visible to every exporter, including those of factories that
/// intentionally opted out of TUnit's correlation. That cross-factory leak is what
/// produced the foreign <c>tunit.test.id</c> tag in run 25572055061.
/// </para>
/// <para>
/// The baggage key written by the per-factory request middleware encodes which factory
/// owns the in-flight request. The processor compares the baggage value to its own
/// scope's <see cref="FactoryId"/> before tagging.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CorrelationScope
{
    /// <summary>Activity baggage key carrying the owning factory's id.</summary>
    public const string FactoryIdBaggageKey = "tunit.factory.id";

    /// <summary>A unique id for the owning factory.</summary>
    public string FactoryId { get; } = Guid.NewGuid().ToString("N");
}
