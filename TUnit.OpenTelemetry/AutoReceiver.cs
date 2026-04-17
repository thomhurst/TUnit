using System.ComponentModel;
using TUnit.Core;
using TUnit.OpenTelemetry.Receiver;

namespace TUnit.OpenTelemetry;

/// <summary>
/// Starts a process-wide OTLP/HTTP receiver at test discovery so that SUT
/// processes (e.g., those started by WebApplicationFactory) can export spans
/// back into the TUnit HTML report. Users opt out by setting the
/// <c>TUNIT_OTEL_RECEIVER</c> environment variable to <c>0</c>.
/// </summary>
public static class AutoReceiver
{
    /// <summary>Hook order for <see cref="Start"/>. Runs early so the receiver
    /// is listening before any test-time code tries to emit telemetry.</summary>
    public const int AutoReceiverOrder = int.MinValue + 1000;

    internal const string AutoReceiverEnvVar = "TUNIT_OTEL_RECEIVER";

    private static OtlpReceiver? _receiver;
    private static readonly Lock _lock = new();

    /// <summary>
    /// The URL of the local OTLP receiver (e.g., <c>http://127.0.0.1:41234</c>), or
    /// <c>null</c> if the receiver is not running. Pass this to SUT processes as the
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> env var to route their telemetry back to TUnit.
    /// </summary>
    public static string? Endpoint
    {
        get
        {
            lock (_lock)
            {
                return _receiver is null ? null : $"http://127.0.0.1:{_receiver.Port}";
            }
        }
    }

    [Before(HookType.TestDiscovery, Order = AutoReceiverOrder)]
    public static void Start()
    {
        if (Environment.GetEnvironmentVariable(AutoReceiverEnvVar) == "0")
        {
            return;
        }

        lock (_lock)
        {
            if (_receiver is not null)
            {
                return;
            }

            var upstream = Environment.GetEnvironmentVariable(AutoStart.OtlpEndpointEnvVar);
            var receiver = new OtlpReceiver(upstreamEndpoint: upstream);
            receiver.Start();
            _receiver = receiver;
        }
    }

    [After(HookType.TestSession, Order = AutoReceiverOrder)]
    public static async Task Stop()
    {
        OtlpReceiver? toDispose;
        lock (_lock)
        {
            toDispose = _receiver;
            _receiver = null;
        }

        if (toDispose is not null)
        {
            await toDispose.DisposeAsync();
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static bool HasReceiverForTesting
    {
        get
        {
            lock (_lock)
            {
                return _receiver is not null;
            }
        }
    }
}
