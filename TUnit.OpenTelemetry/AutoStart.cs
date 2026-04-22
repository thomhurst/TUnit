using System.ComponentModel;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
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

    internal const string AutoStartEnvVar = "TUNIT_OTEL_AUTOSTART";
    internal const string OtlpEndpointEnvVar = "OTEL_EXPORTER_OTLP_ENDPOINT";
    internal const string ServiceNameEnvVar = "OTEL_SERVICE_NAME";

    private static readonly ActivitySource ProbeSource = new("TUnit");
    private static TracerProvider? _provider;
    private static readonly Lock _lock = new();

    [Before(HookType.TestDiscovery, Order = AutoStartOrder)]
    public static void Start()
    {
        var autostart = Environment.GetEnvironmentVariable(AutoStartEnvVar);
        if (autostart == "0")
        {
            return;
        }

        var force = autostart == "1";
        var otlpEndpoint = Environment.GetEnvironmentVariable(OtlpEndpointEnvVar);

        lock (_lock)
        {
            if (_provider is not null)
            {
                return;
            }

            if (!force)
            {
                if (otlpEndpoint is null && !TUnitOpenTelemetry.HasConfiguration)
                {
                    return;
                }

                if (ProbeSource.HasListeners())
                {
                    return;
                }
            }
        }

        var builder = Sdk.CreateTracerProviderBuilder()
            .AddSource("TUnit")
            .AddSource("TUnit.Lifecycle")
            // Runtime-emitted client spans from the test-runner's own HttpClient traffic
            // (e.g. AspireFixture.CreateHttpClient). Emitted by .NET's SocketsHttpHandler
            // pipeline; without this subscription the spans exist but aren't exported, and
            // cross-process traces show orphan-parent server spans on the SUT side.
            .AddSource("System.Net.Http")
            .AddProcessor(new TUnitTestCorrelationProcessor());

        if (otlpEndpoint is not null)
        {
            builder.AddOtlpExporter();
        }

        builder.SetResourceBuilder(
            ResourceBuilder.CreateDefault().AddService(
                serviceName: Environment.GetEnvironmentVariable(ServiceNameEnvVar)
                    ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
                    ?? "TUnit.Tests"));

        TUnitOpenTelemetry.ApplyConfiguration(builder);
        var provider = builder.Build();

        lock (_lock)
        {
            if (_provider is not null)
            {
                // Lost the race — another Start call built first. Dispose ours.
                provider.Dispose();
                return;
            }
            _provider = provider;
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static Resource? GetResourceForTesting()
    {
        lock (_lock)
        {
            return _provider?.GetResource();
        }
    }
}
