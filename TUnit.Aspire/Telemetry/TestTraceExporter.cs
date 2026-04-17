using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TUnit.Core;

namespace TUnit.Aspire.Telemetry;

/// <summary>
/// Helpers that wire the Aspire dashboard's OTLP endpoint into a
/// <see cref="TracerProviderBuilder"/>. The provider itself is owned by
/// <c>TUnit.OpenTelemetry.AutoStart</c>; this class only contributes configuration.
/// </summary>
internal static class TestTraceExporter
{
    private const string DashboardOtlpEndpointEnvVar = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DefaultServiceName = "TUnit.Tests";

    internal static Uri? TryGetDashboardEndpoint()
        => TryParseDashboardEndpoint(Environment.GetEnvironmentVariable(DashboardOtlpEndpointEnvVar));

    internal static void AddToBuilder(TracerProviderBuilder builder, TestSessionContext context, Uri endpoint)
    {
        builder
            .SetResourceBuilder(CreateResourceBuilder(context))
            .AddOtlpExporter(options =>
            {
                options.Endpoint = GetTracesEndpoint(endpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
    }

    /// <summary>
    /// Builds a standalone <see cref="TracerProvider"/> bound to the captured OTLP endpoint.
    /// Used by test-only code paths that need an owned provider; production uses <see cref="AddToBuilder"/>.
    /// </summary>
    internal static TracerProvider CreateTracerProvider(
        Uri endpoint, TestSessionContext context, string sourceName)
    {
        var builder = Sdk.CreateTracerProviderBuilder().AddSource(sourceName);
        AddToBuilder(builder, context, endpoint);
        return builder.Build();
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

    private static ResourceBuilder CreateResourceBuilder(TestSessionContext context)
    {
        var serviceName = GetServiceName(context);
        var serviceVersion = typeof(TestTraceExporter).Assembly.GetName().Version?.ToString();

        return ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion);
    }

    private static string GetServiceName(TestSessionContext context)
    {
        var assemblyNames = context.Assemblies
            .Select(static assemblyContext => assemblyContext.Assembly.GetName().Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToArray();

        return assemblyNames.Length == 1
            ? assemblyNames[0]!
            : DefaultServiceName;
    }
}
