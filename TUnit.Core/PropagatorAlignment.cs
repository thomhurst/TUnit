#if NET

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Auto-aligns <see cref="DistributedContextPropagator.Current"/> to the W3C composite
/// propagator (traceparent + baggage) when the current propagator is .NET's default
/// <c>LegacyPropagator</c>. Without this, cross-process test correlation baggage
/// (<c>tunit.test.id</c>) is emitted as <c>Correlation-Context</c>, which the OpenTelemetry
/// SDK's <c>BaggagePropagator</c> does not read — the baggage silently drops between
/// the test process and the SUT.
/// </summary>
/// <remarks>
/// Set the environment variable <c>TUNIT_KEEP_LEGACY_PROPAGATOR=1</c> to opt out and
/// retain the default LegacyPropagator. Any user-configured propagator that isn't the
/// runtime default is left untouched.
/// </remarks>
internal static class PropagatorAlignment
{
    private const string LegacyPropagatorTypeName = "System.Diagnostics.LegacyPropagator";

    // Read once: env vars don't change within a process and GetEnvironmentVariable allocates.
    private static readonly bool OptedOut =
        Environment.GetEnvironmentVariable("TUNIT_KEEP_LEGACY_PROPAGATOR") == "1";

#pragma warning disable CA2255 // Module initializer is the intended entry point per issue #5592.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void AlignOnModuleLoad() => AlignIfDefault();

    /// <summary>
    /// Idempotent: re-align only if the current propagator is still the runtime default.
    /// </summary>
    internal static void AlignIfDefault()
    {
        if (OptedOut)
        {
            return;
        }

        if (DistributedContextPropagator.Current.GetType().FullName == LegacyPropagatorTypeName)
        {
            DistributedContextPropagator.Current = DistributedContextPropagator.CreateW3CPropagator();
        }
    }
}

#endif
