#if NET

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Auto-aligns <see cref="DistributedContextPropagator.Current"/> to a W3C-compatible
/// propagator (traceparent + W3C baggage) when the current propagator is .NET's default
/// <c>LegacyPropagator</c>. Without this, cross-process test correlation baggage
/// (<c>tunit.test.id</c>) is emitted as <c>Correlation-Context</c>, which the OpenTelemetry
/// SDK's <c>BaggagePropagator</c> does not read — the baggage silently drops between
/// the test process and the SUT.
/// </summary>
/// <remarks>
/// On .NET 10+, delegates to the runtime's <c>DistributedContextPropagator.CreateW3CPropagator()</c>.
/// On .NET 8/9, uses a minimal built-in W3C baggage propagator.
/// Set <c>TUNIT_KEEP_LEGACY_PROPAGATOR=1</c> to opt out.
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
            DistributedContextPropagator.Current = CreateAlignedPropagator();
        }
    }

    /// <summary>
    /// Returns the propagator TUnit aligns to. On .NET 10+ this is the runtime's
    /// built-in W3C propagator; on .NET 8/9 a minimal in-library equivalent.
    /// </summary>
    internal static DistributedContextPropagator CreateAlignedPropagator()
    {
#if NET10_0_OR_GREATER
        return DistributedContextPropagator.CreateW3CPropagator();
#else
        return new W3CBaggagePropagator();
#endif
    }

#if !NET10_0_OR_GREATER
    /// <summary>
    /// Minimal W3C propagator for .NET 8/9: delegates <c>traceparent</c>/<c>tracestate</c>
    /// to the default runtime propagator, and emits/parses baggage using the W3C
    /// <c>baggage</c> header rather than the legacy <c>Correlation-Context</c>.
    /// </summary>
    private sealed class W3CBaggagePropagator : DistributedContextPropagator
    {
        private const string LegacyBaggageHeader = "Correlation-Context";

        private static readonly DistributedContextPropagator DefaultPropagator = CreateDefaultPropagator();
        private static readonly IReadOnlyCollection<string> FieldNames = new[] { "traceparent", "tracestate", TUnitActivitySource.BaggageHeader };

        public override IReadOnlyCollection<string> Fields => FieldNames;

        public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
        {
            if (activity is null || setter is null)
            {
                return;
            }

            // Filter the legacy baggage header from the default propagator's output
            // so we don't emit both Correlation-Context and W3C baggage for the same values.
            DefaultPropagator.Inject(activity, carrier, (c, key, value) =>
            {
                if (!string.Equals(key, LegacyBaggageHeader, StringComparison.OrdinalIgnoreCase))
                {
                    setter(c, key, value);
                }
            });

            if (TUnitActivitySource.TryBuildBaggageHeader(activity) is { } baggage)
            {
                setter(carrier, TUnitActivitySource.BaggageHeader, baggage);
            }
        }

        public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
            => DefaultPropagator.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);

        public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter)
        {
            if (getter is null)
            {
                return null;
            }

            getter(carrier, TUnitActivitySource.BaggageHeader, out var header, out var headers);
            if (string.IsNullOrEmpty(header) && headers is not null)
            {
                foreach (var h in headers)
                {
                    if (!string.IsNullOrEmpty(h))
                    {
                        header = h;
                        break;
                    }
                }
            }

            return string.IsNullOrEmpty(header) ? null : ParseBaggage(header!);
        }

        private static List<KeyValuePair<string, string?>>? ParseBaggage(string header)
        {
            List<KeyValuePair<string, string?>>? result = null;
            var remaining = header.AsSpan();

            while (!remaining.IsEmpty)
            {
                ReadOnlySpan<char> entry;
                var comma = remaining.IndexOf(',');
                if (comma < 0)
                {
                    entry = remaining;
                    remaining = default;
                }
                else
                {
                    entry = remaining[..comma];
                    remaining = remaining[(comma + 1)..];
                }

                entry = entry.Trim();
                var semi = entry.IndexOf(';');
                if (semi >= 0)
                {
                    entry = entry[..semi];
                }

                var eq = entry.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                var key = Uri.UnescapeDataString(entry[..eq].Trim().ToString());
                if (key.Length == 0)
                {
                    continue;
                }

                var value = Uri.UnescapeDataString(entry[(eq + 1)..].Trim().ToString());
                (result ??= new List<KeyValuePair<string, string?>>()).Add(new KeyValuePair<string, string?>(key, value));
            }

            return result;
        }
    }
#endif
}

#endif
