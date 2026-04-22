using TUnit.OpenTelemetry.Receiver;

namespace TUnit.Aspire;

internal static class OtlpEndpointEnvironment
{
    internal const string OtelExporterEndpointEnvVar = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// Captures any user-supplied <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> as the receiver's
    /// upstream forward target, then overrides the env var with <paramref name="overrideEndpoint"/>
    /// so the SUT exports to the local TUnit receiver. Without this, the user's own
    /// dashboard silently loses all SUT spans (#4818).
    /// </summary>
    /// <returns>The captured user endpoint, or <c>null</c> if none was set.</returns>
    public static string? CaptureAndOverride(
        IDictionary<string, object> environmentVariables,
        OtlpReceiver receiver,
        string overrideEndpoint)
    {
        string? userEndpoint = null;

        if (environmentVariables.TryGetValue(OtelExporterEndpointEnvVar, out var existing)
            && existing is string s
            && !string.IsNullOrWhiteSpace(s))
        {
            userEndpoint = s;
            receiver.UpstreamEndpoint = s;
        }

        environmentVariables[OtelExporterEndpointEnvVar] = overrideEndpoint;
        return userEndpoint;
    }
}
