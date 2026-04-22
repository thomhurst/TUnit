namespace TUnit.Aspire;

internal static class OtlpEndpointEnvironment
{
    internal const string OtelExporterEndpointEnvVar = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// Captures any user-supplied <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, then overrides the
    /// env var with <paramref name="overrideEndpoint"/> so the SUT exports to the local
    /// TUnit receiver. The caller is responsible for forwarding the captured value to the
    /// receiver's upstream — without it, the user's own dashboard silently loses all SUT
    /// spans (#4818).
    /// </summary>
    /// <returns>The captured user endpoint, or <c>null</c> if none was set as a string.</returns>
    /// <remarks>
    /// Non-string entries (e.g. <c>EndpointReference</c>, <c>ParameterResource</c>) are not
    /// captured. Aspire would normally resolve those to URLs at process launch, but once
    /// we overwrite the entry that resolution path is lost. Documenting as a known
    /// limitation; revisit if a user reports it.
    /// </remarks>
    internal static string? CaptureAndOverride(
        IDictionary<string, object> environmentVariables,
        string overrideEndpoint)
    {
        string? userEndpoint = null;

        if (environmentVariables.TryGetValue(OtelExporterEndpointEnvVar, out var existing)
            && existing is string s
            && !string.IsNullOrWhiteSpace(s))
        {
            userEndpoint = s;
        }

        environmentVariables[OtelExporterEndpointEnvVar] = overrideEndpoint;
        return userEndpoint;
    }
}
