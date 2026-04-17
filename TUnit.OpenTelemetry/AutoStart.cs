using System.ComponentModel;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using TUnit.Core;

namespace TUnit.OpenTelemetry;

/// <summary>
/// Lifecycle hooks that build a <see cref="TracerProvider"/> around TUnit's activity
/// sources at test discovery and dispose it at session end. Users who already register
/// a listener or TracerProvider in their own <c>[Before(TestDiscovery)]</c> hook keep
/// full control — this class detects existing listeners and steps aside.
/// </summary>
public static class AutoStart
{
    /// <summary>Hook order for <see cref="Start"/>. Runs last so user hooks register first.</summary>
    public const int AutoStartOrder = int.MaxValue;

    private static readonly ActivitySource ProbeSource = new("TUnit");
    private static TracerProvider? _provider;
    private static readonly Lock _lock = new();

    [Before(HookType.TestDiscovery, Order = AutoStartOrder)]
    public static void Start()
    {
        var autostart = Environment.GetEnvironmentVariable("TUNIT_OTEL_AUTOSTART");
        if (autostart == "0")
        {
            return;
        }

        var force = autostart == "1";
        if (!force && ProbeSource.HasListeners())
        {
            return;
        }

        lock (_lock)
        {
            if (_provider is not null)
            {
                return;
            }

            var builder = Sdk.CreateTracerProviderBuilder()
                .AddSource("TUnit")
                .AddSource("TUnit.Lifecycle")
                .AddSource("TUnit.AspNetCore.Http")
                .AddProcessor(new TUnitTestCorrelationProcessor());

            if (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") is not null)
            {
                builder.AddOtlpExporter();
            }

            TUnitOpenTelemetry.ApplyConfiguration(builder);
            _provider = builder.Build();
        }
    }

    [After(HookType.TestSession, Order = int.MaxValue)]
    public static void Stop()
    {
        TracerProvider? toDispose;
        lock (_lock)
        {
            toDispose = _provider;
            _provider = null;
        }

        toDispose?.Dispose();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static void StartForTesting(bool resetFirst)
    {
        if (resetFirst)
        {
            Stop();
        }

        Start();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static bool HasProviderForTesting
    {
        get
        {
            lock (_lock)
            {
                return _provider is not null;
            }
        }
    }
}
