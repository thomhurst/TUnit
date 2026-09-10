using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TUnit.Aspire.Telemetry;

/// <summary>
/// Helpers that wire the Aspire dashboard's OTLP endpoint into a
/// <see cref="TracerProviderBuilder"/>. The provider itself is owned by
/// <c>TUnit.OpenTelemetry.AutoStart</c>; this class only contributes configuration.
/// </summary>
internal static class TestTraceExporter
{
    private const string DashboardOtlpEndpointEnvVar = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string ServiceNameEnvVar = "OTEL_SERVICE_NAME";
    private const string DefaultServiceName = "TUnit.Tests";

    internal static Uri? TryGetDashboardEndpoint()
        => TryParseDashboardEndpoint(Environment.GetEnvironmentVariable(DashboardOtlpEndpointEnvVar));

    internal static void AddToBuilder(TracerProviderBuilder builder, Uri endpoint)
    {
        builder
            .SetResourceBuilder(CreateResourceBuilder())
            .AddOtlpExporter(options =>
            {
                options.Endpoint = GetTracesEndpoint(endpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
    }

    internal static Uri? TryParseDashboardEndpoint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : null;
    }

    internal static Uri GetTracesEndpoint(Uri endpoint)
    {
        var builder = new UriBuilder(endpoint);
        var path = builder.Path.TrimEnd('/');

        if (!path.EndsWith("/v1/traces", StringComparison.OrdinalIgnoreCase))
        {
            builder.Path = string.IsNullOrEmpty(path)
                ? "/v1/traces"
                : $"{path}/v1/traces";
        }

        return builder.Uri;
    }

    private static ResourceBuilder CreateResourceBuilder()
    {
        var serviceName = GetServiceName();
        var serviceVersion = typeof(TestTraceExporter).Assembly.GetName().Version?.ToString();

        return ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion);
    }

    private static string GetServiceName()
    {
        var fromEnv = Environment.GetEnvironmentVariable(ServiceNameEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv!;
        }

        return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
            ?? DefaultServiceName;
    }
}
