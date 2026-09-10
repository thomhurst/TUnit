using TUnit.Aspire;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

public class OtlpEndpointEnvironmentTests
{
    private const string EnvVar = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string OverrideEndpoint = "http://127.0.0.1:5000";

    [Test]
    public async Task Capture_ReturnsUserEndpoint_AndOverridesValue()
    {
        // Regression for #4818: when the user has already set OTEL_EXPORTER_OTLP_ENDPOINT
        // (e.g. pointing at their own Aspire dashboard via WithEnvironment in ConfigureBuilder),
        // TUnit must surface that endpoint to the caller (which then forwards it to the
        // receiver) before overriding the env var with the local TUnit receiver URL —
        // otherwise the user's dashboard silently loses all SUT spans.
        var env = new Dictionary<string, object>
        {
            [EnvVar] = "http://my-dashboard:18889",
        };

        var captured = OtlpEndpointEnvironment.CaptureAndOverride(env, OverrideEndpoint);

        await Assert.That(captured).IsEqualTo("http://my-dashboard:18889");
        await Assert.That(env[EnvVar] as string).IsEqualTo(OverrideEndpoint);
    }

    [Test]
    public async Task Capture_NoUserEndpoint_ReturnsNull_AndSetsOverride()
    {
        var env = new Dictionary<string, object>();

        var captured = OtlpEndpointEnvironment.CaptureAndOverride(env, OverrideEndpoint);

        await Assert.That(captured).IsNull();
        await Assert.That(env[EnvVar] as string).IsEqualTo(OverrideEndpoint);
    }

    [Test]
    public async Task Capture_IgnoresNonStringExistingValue_ButStillOverrides()
    {
        // Aspire env-var dict values can be EndpointReference, ParameterResource, etc.
        // Only plain string values are forward-target candidates — non-strings are opaque
        // and cannot be used as a URL. The override still happens so the SUT exports to
        // the local receiver regardless.
        var env = new Dictionary<string, object>
        {
            [EnvVar] = new object(),
        };

        var captured = OtlpEndpointEnvironment.CaptureAndOverride(env, OverrideEndpoint);

        await Assert.That(captured).IsNull();
        await Assert.That(env[EnvVar] as string).IsEqualTo(OverrideEndpoint);
    }
}
