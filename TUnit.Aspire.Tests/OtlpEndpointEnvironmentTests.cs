using TUnit.Aspire;
using TUnit.OpenTelemetry.Receiver;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

public class OtlpEndpointEnvironmentTests
{
    [Test]
    public async Task Capture_PreservesUserEndpoint_AsReceiverUpstream()
    {
        // Regression for #4818: when the user has already set OTEL_EXPORTER_OTLP_ENDPOINT
        // (e.g. pointing at their own Aspire dashboard via WithEnvironment in ConfigureBuilder),
        // TUnit must capture that endpoint as the receiver's upstream forward target before
        // overriding the env var with the local OtlpReceiver URL — otherwise spans are
        // silently dropped from the dashboard.
        await using var receiver = new OtlpReceiver();
        var env = new Dictionary<string, object>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://my-dashboard:18889",
        };

        var captured = OtlpEndpointEnvironment.CaptureAndOverride(
            env,
            receiver,
            overrideEndpoint: "http://127.0.0.1:5000");

        await Assert.That(captured).IsEqualTo("http://my-dashboard:18889");
        await Assert.That(receiver.UpstreamEndpoint).IsEqualTo("http://my-dashboard:18889");
        await Assert.That(env["OTEL_EXPORTER_OTLP_ENDPOINT"] as string).IsEqualTo("http://127.0.0.1:5000");
    }

    [Test]
    public async Task Capture_NoUserEndpoint_LeavesUpstreamNull()
    {
        await using var receiver = new OtlpReceiver();
        var env = new Dictionary<string, object>();

        var captured = OtlpEndpointEnvironment.CaptureAndOverride(
            env,
            receiver,
            overrideEndpoint: "http://127.0.0.1:5000");

        await Assert.That(captured).IsNull();
        await Assert.That(receiver.UpstreamEndpoint).IsNull();
        await Assert.That(env["OTEL_EXPORTER_OTLP_ENDPOINT"] as string).IsEqualTo("http://127.0.0.1:5000");
    }

    [Test]
    public async Task Capture_IgnoresNonStringExistingValue()
    {
        // Aspire env-var dict values can be EndpointReference, ParameterResource, etc.
        // Only plain string values should be treated as forward targets — anything else
        // is opaque and would not have resolved to a usable URL anyway.
        await using var receiver = new OtlpReceiver();
        var env = new Dictionary<string, object>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = new object(),
        };

        var captured = OtlpEndpointEnvironment.CaptureAndOverride(
            env,
            receiver,
            overrideEndpoint: "http://127.0.0.1:5000");

        await Assert.That(captured).IsNull();
        await Assert.That(receiver.UpstreamEndpoint).IsNull();
    }
}
