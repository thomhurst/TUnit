using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TUnit.Core;

namespace TUnit.Aspire.Telemetry;

/// <summary>
/// Exports TUnit's per-test spans to the same OTLP backend used by the Aspire dashboard.
/// This keeps backend trace trees navigable without requiring users to configure a separate
/// tracer provider in their test project.
/// </summary>
internal static class TestTraceExporter
{
    private const string DashboardOtlpEndpointEnvVar = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string TUnitSourceName = "TUnit";
    private const string DefaultServiceName = "TUnit.Tests";

    private static readonly Lock SyncLock = new();
    private static TracerProvider? _tracerProvider;

    internal static bool IsStarted
    {
        get
        {
            lock (SyncLock)
            {
                return _tracerProvider is not null;
            }
        }
    }

    internal static void TryStart(TestSessionContext context)
    {
        var endpoint = TryParseDashboardEndpoint(
            Environment.GetEnvironmentVariable(DashboardOtlpEndpointEnvVar));

        if (endpoint is null)
        {
            return;
        }

        lock (SyncLock)
        {
            if (_tracerProvider is not null)
            {
                return;
            }

            _tracerProvider = CreateTracerProvider(endpoint, context, TUnitSourceName);
        }
    }

    /// <summary>
    /// Builds a standalone <see cref="TracerProvider"/> for the given endpoint and session.
    /// Caller owns disposal. Used by <see cref="TryStart"/> for the singleton case and by
    /// tests that need an isolated provider (parallel-safe — no static state touched).
    /// </summary>
    internal static TracerProvider CreateTracerProvider(
        Uri endpoint, TestSessionContext context, string sourceName)
    {
        return Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(CreateResourceBuilder(context))
            .AddSource(sourceName)
            .AddOtlpExporter(options =>
            {
                options.Endpoint = GetTracesEndpoint(endpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            })
            .Build();
    }

    internal static void Stop()
    {
        TracerProvider? providerToDispose = null;

        lock (SyncLock)
        {
            if (_tracerProvider is null)
            {
                return;
            }

            providerToDispose = _tracerProvider;
            _tracerProvider = null;
        }

        providerToDispose.Dispose();
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
