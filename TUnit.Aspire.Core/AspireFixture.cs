using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.OpenTelemetry.Receiver;

namespace TUnit.Aspire;

/// <summary>
/// A fixture that manages the lifecycle of an Aspire distributed application for testing.
/// Implements <see cref="IAsyncInitializer"/> and <see cref="IAsyncDisposable"/> to integrate
/// with TUnit's lifecycle automatically.
/// </summary>
/// <typeparam name="TAppHost">The Aspire AppHost project type (e.g., <c>Projects.MyAppHost</c>).</typeparam>
/// <remarks>
/// <para>
/// Use directly with <c>[ClassDataSource&lt;AspireFixture&lt;Projects.MyAppHost&gt;&gt;(Shared = SharedType.PerTestSession)]</c>
/// or subclass to customize behavior via the virtual configuration hooks.
/// </para>
/// </remarks>
public class AspireFixture<TAppHost> : IAsyncInitializer, IAsyncDisposable
    where TAppHost : class
{
    private DistributedApplication? _app;
    private OtlpReceiver? _otlpReceiver;

    // Captured state-transition timeline (recorded by the background monitor), folded into the
    // exception when startup fails so the failure tells the whole story in one place.
    private readonly ConcurrentQueue<StateTransition> _timeline = new();
    private Stopwatch? _timelineClock;

    /// <summary>
    /// The running Aspire distributed application.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="InitializeAsync"/> completes.</exception>
    public DistributedApplication App => _app ?? throw new InvalidOperationException(
        "App not initialized. Ensure InitializeAsync has completed.");

    /// <summary>
    /// Creates an <see cref="HttpClient"/> for the named resource via the AppHost's
    /// <see cref="IHttpClientFactory"/>, so any <see cref="DelegatingHandler"/> registered
    /// in <see cref="ConfigureBuilder"/> participates in the pipeline.
    /// </summary>
    /// <param name="resourceName">The name of the resource to connect to.</param>
    /// <param name="endpointName">Optional endpoint name if the resource exposes multiple endpoints.</param>
    /// <returns>An <see cref="HttpClient"/> configured to connect to the resource.</returns>
    public HttpClient CreateHttpClient(string resourceName, string? endpointName = null)
        => App.CreateHttpClient(resourceName, endpointName);

    /// <summary>
    /// Gets the connection string for the named resource.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The connection string, or <c>null</c> if not available.</returns>
    public async Task<string?> GetConnectionStringAsync(string resourceName, CancellationToken cancellationToken = default)
        => await App.GetConnectionStringAsync(resourceName, cancellationToken);

    /// <summary>
    /// Subscribes to logs from the named resource and routes them to the current test's output.
    /// Dispose the returned value to stop watching.
    /// </summary>
    /// <param name="resourceName">The name of the resource to watch logs for.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that stops the log subscription when disposed.</returns>
    /// <exception cref="InvalidOperationException">Thrown if called outside of a test context.</exception>
    public IAsyncDisposable WatchResourceLogs(string resourceName)
    {
        var testContext = TestContext.Current ?? throw new InvalidOperationException(
            "WatchResourceLogs must be called from within a test.");

        var cts = new CancellationTokenSource();
        var loggerService = App.Services.GetRequiredService<ResourceLoggerService>();

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var batch in loggerService.WatchAsync(resourceName)
                    .WithCancellation(cts.Token))
                {
                    foreach (var line in batch)
                    {
                        testContext.Output.WriteLine($"[{resourceName}] {line.Content}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the watcher is disposed
            }
        });

        return new ResourceLogWatcher(cts);
    }

    // --- Configuration hooks (virtual) ---

    private static readonly Stream StdErr = Console.OpenStandardError();
    private static readonly object StdErrGate = new();

    /// <summary>
    /// Logs progress messages during initialization. Override to route to a custom logger.
    /// Default implementation writes directly to the standard error stream, bypassing any
    /// interceptors or buffering, for immediate CI visibility.
    /// </summary>
    /// <param name="message">The progress message.</param>
    protected virtual void LogProgress(string message)
    {
        var bytes = Encoding.UTF8.GetBytes($"[Aspire] {message}{Environment.NewLine}");

        // The background resource monitor and the main init thread both log concurrently, and
        // separate fixture instances share this static stream — lock so lines never splice.
        lock (StdErrGate)
        {
            StdErr.Write(bytes, 0, bytes.Length);
            StdErr.Flush();
        }
    }

    /// <summary>
    /// Command-line arguments to pass to the AppHost entry point.
    /// These are forwarded to <see cref="DistributedApplicationTestingBuilder.CreateAsync{TEntryPoint}(string[], CancellationToken)"/>
    /// and are available in the AppHost's <c>builder.Configuration</c> before the builder is created.
    /// </summary>
    /// <remarks>
    /// Use this to pass configuration values that are consumed during AppHost builder creation:
    /// <code>
    /// protected override string[] Args => [
    ///     "--UseVolumes=false",
    ///     "--UsePostgresWithSessionLifetime=true"
    /// ];
    /// </code>
    /// For configuration that can be set after builder creation, use <see cref="ConfigureBuilder"/> instead.
    /// </remarks>
    protected virtual string[] Args => [];

    /// <summary>
    /// Override to configure the <see cref="DistributedApplicationOptions"/> and
    /// <see cref="Microsoft.Extensions.Hosting.HostApplicationBuilderSettings"/> passed to
    /// <see cref="DistributedApplicationTestingBuilder.CreateAsync{TEntryPoint}(string[], Action{DistributedApplicationOptions, Microsoft.Extensions.Hosting.HostApplicationBuilderSettings}, CancellationToken)"/>.
    /// This callback is invoked during builder creation, before <see cref="ConfigureBuilder"/>.
    /// </summary>
    /// <param name="options">The distributed application options.</param>
    /// <param name="settings">The host application builder settings.</param>
    protected virtual void ConfigureAppHost(DistributedApplicationOptions options, HostApplicationBuilderSettings settings) { }

    /// <summary>
    /// Override to customize the builder before building the application.
    /// </summary>
    /// <param name="builder">The distributed application testing builder.</param>
    protected virtual void ConfigureBuilder(IDistributedApplicationTestingBuilder builder) { }

    /// <summary>
    /// When <c>true</c>, starts an OTLP receiver that collects telemetry from SUT resources
    /// and routes correlated logs to the originating test's output. SUT resources must have
    /// OpenTelemetry configured (e.g., via Aspire's standard ServiceDefaults) — no TUnit-specific
    /// code is needed in the SUT.
    /// </summary>
    /// <remarks>
    /// The receiver overrides <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> on project resources. If the
    /// Aspire dashboard is enabled, received telemetry is forwarded to the dashboard's original
    /// OTLP endpoint so both TUnit and the dashboard receive data.
    /// </remarks>
    protected virtual bool EnableTelemetryCollection => true;

    /// <summary>
    /// Resource wait timeout. Default: 60 seconds.
    /// </summary>
    protected virtual TimeSpan ResourceTimeout => TimeSpan.FromSeconds(60);

    /// <summary>
    /// Which resources to wait for. Default: <see cref="ResourceWaitBehavior.AllHealthy"/>.
    /// </summary>
    protected virtual ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.AllHealthy;

    /// <summary>
    /// Resources to wait for when <see cref="WaitBehavior"/> is <see cref="ResourceWaitBehavior.Named"/>.
    /// </summary>
    protected virtual IEnumerable<string> ResourcesToWaitFor() => [];

    /// <summary>
    /// Resources to be removed from the <see cref="DistributedApplicationTestingBuilder"/>.
    /// </summary>
    protected virtual IEnumerable<string> ResourcesToRemove() => [];

    /// <summary>
    /// Override for full control over the resource waiting logic.
    /// </summary>
    /// <param name="app">The running distributed application.</param>
    /// <param name="cancellationToken">A cancellation token that will be cancelled after <see cref="ResourceTimeout"/>.</param>
    protected virtual async Task WaitForResourcesAsync(DistributedApplication app, CancellationToken cancellationToken)
    {
        var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();

        switch (WaitBehavior)
        {
            case ResourceWaitBehavior.AllHealthy:
            {
                var model = app.Services.GetRequiredService<DistributedApplicationModel>();
                var names = GetWaitableResourceNames(model);
                await WaitForResourcesWithFailFastAsync(app, notificationService, names, waitForHealthy: true, cancellationToken);
                break;
            }

            case ResourceWaitBehavior.AllRunning:
            {
                var model = app.Services.GetRequiredService<DistributedApplicationModel>();
                var names = GetWaitableResourceNames(model);
                await WaitForResourcesWithFailFastAsync(app, notificationService, names, waitForHealthy: false, cancellationToken);
                break;
            }

            case ResourceWaitBehavior.Named:
            {
                var names = ResourcesToWaitFor().ToList();
                await WaitForResourcesWithFailFastAsync(app, notificationService, names, waitForHealthy: true, cancellationToken);
                break;
            }

            case ResourceWaitBehavior.None:
                break;
        }
    }

    // --- Lifecycle ---

    /// <summary>
    /// Initializes the Aspire distributed application. Override to add post-start
    /// logic such as database migrations or data seeding:
    /// <code>
    /// public override async Task InitializeAsync()
    /// {
    ///     await base.InitializeAsync();
    ///     await RunMigrationsAsync();
    /// }
    /// </code>
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        var sw = Stopwatch.StartNew();

        // Start OTLP receiver before building the app so we can inject the endpoint
        if (EnableTelemetryCollection)
        {
            LogProgress("Starting OTLP telemetry receiver...");
            StartOtlpReceiver();
            LogProgress($"OTLP receiver listening on port {_otlpReceiver!.Port}");
        }

        LogProgress($"Creating distributed application builder for {typeof(TAppHost).Name}...");
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TAppHost>(Args, ConfigureAppHost);
        ConfigureBuilder(builder);
        RemoveResources(builder);

        // Configure OTLP endpoint on project resources AFTER user's ConfigureBuilder
        if (_otlpReceiver is not null)
        {
            ConfigureOtlpEndpoints(builder);
        }

        LogProgress($"Builder created in {sw.Elapsed.TotalSeconds:0.0}s");

        LogProgress("Building application...");
        _app = await builder.BuildAsync();
        LogProgress($"Application built in {sw.Elapsed.TotalSeconds:0.0}s");

        var model = _app.Services.GetRequiredService<DistributedApplicationModel>();
        var resourceList = string.Join(", ", model.Resources.Select(r => r.Name));
        LogProgress($"Starting application with resources: [{resourceList}]");

        // Monitor resource state changes in the background, covering BOTH startup and the
        // resource-wait phase, so the captured timeline includes health-check-wait hangs.
        // This also provides real-time visibility into container health check failures, SSL
        // errors, etc. that would otherwise be invisible until a timeout fires.
        _timelineClock = Stopwatch.StartNew();
        using var monitorCts = new CancellationTokenSource();
        var monitorTask = MonitorResourceEventsAsync(_app, monitorCts.Token);
        var notificationService = _app.Services.GetRequiredService<ResourceNotificationService>();

        try
        {
            using (var startCts = new CancellationTokenSource(ResourceTimeout))
            {
                try
                {
                    await _app.StartAsync(startCts.Token);
                }
                catch (OperationCanceledException) when (startCts.IsCancellationRequested)
                {
                    var headline = $"Timed out after {ResourceTimeout.TotalSeconds:0}s waiting for the Aspire application to start.";
                    throw new TimeoutException(
                        await BuildDiagnosticsAndAttachAsync(_app, notificationService, headline,
                            model.Resources.Select(r => r.Name).ToList()));
                }
            }

            LogProgress($"Application started in {sw.Elapsed.TotalSeconds:0.0}s. Waiting for resources (timeout: {ResourceTimeout.TotalSeconds:0}s, behavior: {WaitBehavior})...");

            using (var cts = new CancellationTokenSource(ResourceTimeout))
            {
                try
                {
                    await WaitForResourcesAsync(_app, cts.Token);
                    LogProgress("All resources ready.");
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    // Fallback for custom WaitForResourcesAsync overrides that don't use the default
                    // fail-fast helper (which throws its own rich TimeoutException first).
                    var headline = $"Timed out after {ResourceTimeout.TotalSeconds:0}s waiting for Aspire resources to be ready (wait behavior: {WaitBehavior}).";
                    throw new TimeoutException(
                        await BuildDiagnosticsAndAttachAsync(_app, notificationService, headline,
                            model.Resources.Select(r => r.Name).ToList()));
                }
            }
        }
        finally
        {
            await StopMonitorAsync(monitorCts, monitorTask);
        }
    }

    private void RemoveResources(IDistributedApplicationTestingBuilder builder)
    {
        foreach (var name in ResourcesToRemove())
        {
            var resource =
                builder.Resources.SingleOrDefault(r =>
                    string.Equals(r.Name, name, StringComparison.Ordinal));

            if (resource is not null)
            {
                builder.Resources.Remove(resource);
            }
            else
            {
                LogProgress($"ResourcesToRemove: resource '{name}' not found (skipped).");
            }
        }
    }

    /// <summary>
    /// Disposes the Aspire distributed application. Override to add custom cleanup:
    /// <code>
    /// public override async ValueTask DisposeAsync()
    /// {
    ///     // Custom cleanup before app stops
    ///     await base.DisposeAsync();
    /// }
    /// </code>
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        if (_otlpReceiver is not null && _app is not null)
        {
            // Give the SUT's BatchSpanProcessor a chance to flush trailing spans while the
            // AppHost is still up. Without this, fast tests stop the app before the worker's
            // exporter ticks (default 1s after our OTEL_BSP_SCHEDULE_DELAY override) and the
            // last set of spans never reaches us.
            LogProgress("Draining OTLP receiver...");
            await _otlpReceiver.DrainAsync();
        }

        if (_app is not null)
        {
            LogProgress("Stopping application...");
            await _app.StopAsync();
            LogProgress("Disposing application...");
            await _app.DisposeAsync();
            _app = null;
            LogProgress("Application disposed.");
        }

        if (_otlpReceiver is not null)
        {
            await _otlpReceiver.DisposeAsync();
            _otlpReceiver = null;
        }

        GC.SuppressFinalize(this);
    }

    // --- OTLP Telemetry ---

    private const string DashboardOtlpEndpointEnvVar = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardOtlpHttpEndpointEnvVar = "DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL";
    private const string OtelExporterHeadersEnvVar = "OTEL_EXPORTER_OTLP_HEADERS";
    private const string OtelExporterProtocolEnvVar = "OTEL_EXPORTER_OTLP_PROTOCOL";
    private const string OtelServiceNameEnvVar = "OTEL_SERVICE_NAME";
    private const string OtelBlrpScheduleDelayEnvVar = "OTEL_BLRP_SCHEDULE_DELAY";
    private const string OtelBspScheduleDelayEnvVar = "OTEL_BSP_SCHEDULE_DELAY";

    private void StartOtlpReceiver()
    {
        // Prefer the dashboard's HTTP/protobuf endpoint — the receiver only forwards
        // http/protobuf, not gRPC, so DOTNET_DASHBOARD_OTLP_ENDPOINT_URL (which is gRPC by
        // default) would fail every forward. Fall back to the gRPC endpoint only as a
        // last resort so users with a custom dashboard config still see *something*.
        var upstreamEndpoint = Environment.GetEnvironmentVariable(DashboardOtlpHttpEndpointEnvVar)
            ?? Environment.GetEnvironmentVariable(DashboardOtlpEndpointEnvVar);

        _otlpReceiver = new OtlpReceiver(upstreamEndpoint)
        {
            UpstreamHeaders = ParseOtlpHeaders(Environment.GetEnvironmentVariable(OtelExporterHeadersEnvVar)),
        };
        _otlpReceiver.Start();
    }

    /// <summary>
    /// Parses the <c>OTEL_EXPORTER_OTLP_HEADERS</c> format (<c>key1=value1,key2=value2</c>)
    /// into a header list. The Aspire dashboard requires <c>x-otlp-api-key</c> on its OTLP
    /// endpoints when auth is enabled (the default since Aspire 9), so without forwarding
    /// these the dashboard rejects every proxied span.
    /// </summary>
    private static IReadOnlyDictionary<string, string>? ParseOtlpHeaders(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Split on the first '=' only — base64 tokens (api keys) commonly carry trailing
            // '=' padding that must stay in the value, and the OTEL spec permits empty values
            // (key=) so we don't reject them.
            var eq = pair.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = pair[..eq].Trim();
            var value = pair[(eq + 1)..].Trim();
            if (key.Length > 0)
            {
                result[key] = value;
            }
        }

        return result.Count == 0 ? null : result;
    }

    private void ConfigureOtlpEndpoints(IDistributedApplicationTestingBuilder builder)
    {
        var otlpEndpoint = $"http://127.0.0.1:{_otlpReceiver!.Port}";
        var receiver = _otlpReceiver;

        foreach (var resource in builder.Resources)
        {
            if (resource is ProjectResource projectResource)
            {
                var resourceName = projectResource.Name;

                projectResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation(context =>
                    {
                        // Forward to user's dashboard if they set OTEL_EXPORTER_OTLP_ENDPOINT
                        // themselves (#4818), otherwise their spans get dropped by the override.
                        var userEndpoint = OtlpEndpointEnvironment.CaptureAndOverride(
                            context.EnvironmentVariables,
                            otlpEndpoint);
                        if (userEndpoint is not null)
                        {
                            receiver.UpstreamEndpoint = userEndpoint;
                        }

                        context.EnvironmentVariables[OtelExporterProtocolEnvVar] = "http/protobuf";

                        // Set the service name from the Aspire resource name so
                        // the SUT doesn't need .ConfigureResource(r => r.AddService(...)).
                        // Code-level configuration in the SUT takes precedence.
                        context.EnvironmentVariables.TryAdd(OtelServiceNameEnvVar, resourceName);

                        // Reduce batch export delays for faster test feedback.
                        // Default SDK value is 5000ms which adds unnecessary latency in tests.
                        context.EnvironmentVariables.TryAdd(OtelBlrpScheduleDelayEnvVar, "1000");
                        context.EnvironmentVariables.TryAdd(OtelBspScheduleDelayEnvVar, "1000");
                    }));
            }
        }
    }

    /// <summary>
    /// Monitors resource notification events during startup, logging state transitions
    /// in real time. This provides immediate visibility into issues like health check
    /// failures, SSL errors, or containers stuck in a restart loop — problems that would
    /// otherwise be invisible until a timeout fires.
    /// </summary>
    private async Task MonitorResourceEventsAsync(DistributedApplication app, CancellationToken cancellationToken)
    {
        try
        {
            var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();

            // Single-consumer loop, so plain dictionaries are safe here.
            var trackedStates = new Dictionary<string, string>();
            var trackedHealth = new Dictionary<string, string>();

            await foreach (var evt in notificationService.WatchAsync(cancellationToken))
            {
                var name = evt.Resource.Name;
                var state = evt.Snapshot.State?.Text;

                if (state is null)
                {
                    continue;
                }

                var previousState = trackedStates.GetValueOrDefault(name);
                var stateChanged = state != previousState;

                var healthSignature = HealthSignature(evt.Snapshot.HealthReports);
                var healthChanged = healthSignature != trackedHealth.GetValueOrDefault(name);

                // Only log to stderr when the state text actually changes (unchanged behavior).
                if (stateChanged)
                {
                    trackedStates[name] = state;
                    LogProgress($"  [{name}] {previousState ?? "unknown"} -> {state}");
                }

                // Record a timeline entry on a state OR health transition so the captured story
                // includes health-check deltas (HealthStatus is null off-Running, so diff reports).
                if ((stateChanged || healthChanged) && _timelineClock is { } clock)
                {
                    trackedHealth[name] = healthSignature;
                    RecordTransition(new StateTransition(
                        clock.Elapsed, name, previousState ?? "(none)", state,
                        healthChanged ? $"health: {healthSignature}" : null));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when monitoring is stopped
        }
        catch (Exception ex)
        {
            // Don't let monitoring failures break startup
            LogProgress($"  (resource monitoring stopped: {ex.Message})");
        }
    }

    private void RecordTransition(StateTransition transition)
    {
        _timeline.Enqueue(transition);

        // Bound the timeline so a flapping resource can't grow it without limit.
        while (_timeline.Count > 200 && _timeline.TryDequeue(out _))
        {
        }
    }

    private static string HealthSignature(ImmutableArray<HealthReportSnapshot> reports)
    {
        if (reports.IsDefaultOrEmpty)
        {
            return "none";
        }

        var worst = HealthStatus.Healthy;
        var anyPending = false;

        foreach (var report in reports)
        {
            if (report.Status is { } status)
            {
                if (status < worst)
                {
                    worst = status;
                }
            }
            else
            {
                anyPending = true;
            }
        }

        return anyPending && worst == HealthStatus.Healthy ? "Pending" : worst.ToString();
    }

    private static async Task StopMonitorAsync(CancellationTokenSource monitorCts, Task monitorTask)
    {
        await monitorCts.CancelAsync();

        try
        {
            await monitorTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    /// <summary>
    /// Waits for all named resources to reach the desired state, while simultaneously
    /// watching for any resource entering FailedToStart. If a resource fails, throws
    /// immediately with its recent logs. On timeout, reports which resources are still
    /// pending and includes their logs.
    /// </summary>
    private async Task WaitForResourcesWithFailFastAsync(
        DistributedApplication app,
        ResourceNotificationService notificationService,
        List<string> resourceNames,
        bool waitForHealthy,
        CancellationToken cancellationToken)
    {
        if (resourceNames.Count == 0)
        {
            return;
        }

        // Runtime safety net: skip resources whose snapshot is marked IsHidden.
        // This catches hidden internal resources (e.g. ProjectRebuilderResource registered with
        // IsHidden = true) even if they slip past the model-level ShouldWaitForResource filter.
        var visibleNames = new List<string>(resourceNames.Count);
        List<string>? hiddenNames = null;

        foreach (var name in resourceNames)
        {
            if (notificationService.TryGetCurrentState(name, out var ev) && ev.Snapshot.IsHidden)
            {
                hiddenNames ??= [];
                hiddenNames.Add(name);
            }
            else
            {
                visibleNames.Add(name);
            }
        }

        if (hiddenNames is { Count: > 0 })
        {
            LogProgress($"Skipping {hiddenNames.Count} hidden resource(s) at runtime: [{string.Join(", ", hiddenNames)}]");
        }

        if (visibleNames.Count == 0)
        {
            return;
        }

        // Track which resources have become ready (for timeout reporting)
        var readyResources = new ConcurrentBag<string>();

        // Linked CTS lets us cancel the failure watchers once all resources are ready
        using var failureCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var targetState = waitForHealthy ? "healthy" : "running";
        LogProgress($"Waiting for {visibleNames.Count} resource(s) to become {targetState}: [{string.Join(", ", visibleNames)}]");

        // Success path: wait for all resources to reach the desired state
        var readyTask = Task.WhenAll(visibleNames.Select(async name =>
        {
            if (waitForHealthy)
            {
                await notificationService.WaitForResourceHealthyAsync(name, cancellationToken);
            }
            else
            {
                await notificationService.WaitForResourceAsync(name, KnownResourceStates.Running, cancellationToken);
            }

            readyResources.Add(name);
            LogProgress($"  Resource '{name}' is {targetState} ({readyResources.Count}/{visibleNames.Count})");
        }));

        // Fail-fast path: complete as soon as ANY resource enters a terminal failure state.
        // Watch Exited too — a container that crash-exits cleanly never reports FailedToStart,
        // so without it we would wait the full timeout for an already-dead resource.
        string[] failureStates = [KnownResourceStates.FailedToStart, KnownResourceStates.Exited];
        var failureTasks = visibleNames.Select(async name =>
        {
            await notificationService.WaitForResourceAsync(name, failureStates, failureCts.Token);
            return name;
        }).ToList();
        var anyFailureTask = Task.WhenAny(failureTasks);

        try
        {
            // Race: all resources ready vs. any resource failed
            var completed = await Task.WhenAny(readyTask, anyFailureTask);

            if (completed == anyFailureTask)
            {
                failureCts.Cancel();
                var failedName = await await anyFailureTask;

                var headline = $"Resource '{failedName}' failed to start.";
                throw new InvalidOperationException(
                    await BuildDiagnosticsAndAttachAsync(
                        app, notificationService, headline, [failedName], readyResources.ToArray()));
            }

            // All resources are ready - cancel the failure watchers
            failureCts.Cancel();
            await readyTask;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Timeout - diagnose each pending resource (state, exit code, health, dependencies)
            // and attach the full timeline + untruncated logs as an artifact.
            failureCts.Cancel();

            var readySet = new HashSet<string>(readyResources);
            var pending = visibleNames.Where(n => !readySet.Contains(n)).ToList();

            var headline = $"Timed out after {ResourceTimeout.TotalSeconds:0}s waiting for resource(s) to become {targetState}.";
            throw new TimeoutException(
                await BuildDiagnosticsAndAttachAsync(app, notificationService, headline, pending, readySet));
        }
    }

    /// <summary>
    /// Collects buffered log lines from a resource via the <see cref="ResourceLoggerService"/>.
    /// Returns the raw line content (error/stderr lines prefixed with <c>E&gt;</c>), or an empty
    /// list if none are available. Uses a short timeout to avoid hanging if the log service is
    /// unresponsive; <see cref="ResourceLoggerService.WatchAsync(string)"/> replays the buffered
    /// backlog first, so logs from an already-exited resource are still captured.
    /// </summary>
    private static async Task<List<string>> CollectResourceLogLinesAsync(
        DistributedApplication app,
        string resourceName)
    {
        var loggerService = app.Services.GetRequiredService<ResourceLoggerService>();
        var lines = new List<string>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await foreach (var batch in loggerService.WatchAsync(resourceName)
                .WithCancellation(cts.Token))
            {
                foreach (var line in batch)
                {
                    lines.Add(line.IsErrorMessage ? $"E> {line.Content}" : line.Content);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected - we use a short timeout to collect buffered logs.
        }

        return lines;
    }

    /// <summary>
    /// Returns whether <paramref name="resource"/> should be included in the wait list.
    /// Override to add custom inclusion/exclusion logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: waits for <see cref="IComputeResource"/> (containers, projects, executables) unless
    /// the resource also implements <see cref="IResourceWithParent"/>. Child resources are assumed to
    /// be internally managed by Aspire and should not be independently awaited.
    /// </para>
    /// <para>
    /// For example, Aspire 13.2.0+ adds <c>ProjectRebuilderResource</c> (an <see cref="IComputeResource"/>
    /// that implements <see cref="IResourceWithParent"/>) for hot-reload support. It never emits a
    /// healthy/running state and would cause a guaranteed timeout if waited on.
    /// </para>
    /// <para>
    /// Non-compute resources (parameters, connection strings) are always excluded because they never
    /// report healthy and would hang indefinitely.
    /// </para>
    /// <example>
    /// Override to exclude a slow resource from the wait list:
    /// <code>
    /// protected override bool ShouldWaitForResource(IResource resource)
    ///     => base.ShouldWaitForResource(resource) &amp;&amp; resource.Name != "slow-service";
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="resource">The resource to evaluate.</param>
    /// <returns><see langword="true"/> if the resource should be waited for; otherwise <see langword="false"/>.</returns>
    protected virtual bool ShouldWaitForResource(IResource resource)
        => resource is IComputeResource && resource is not IResourceWithParent;

    private List<string> GetWaitableResourceNames(DistributedApplicationModel model)
    {
        var waitable = new List<string>();
        List<string>? skipped = null;

        foreach (var r in model.Resources)
        {
            if (ShouldWaitForResource(r))
            {
                waitable.Add(r.Name);
            }
            else
            {
                skipped ??= [];
                skipped.Add(r.Name);
            }
        }

        if (skipped is { Count: > 0 })
        {
            LogProgress($"Skipping {skipped.Count} non-waitable resource(s): [{string.Join(", ", skipped)}]");
        }

        return waitable;
    }

    // --- Diagnostics ---

    /// <summary>Why a resource failed or never became ready — used to attach an actionable hint.</summary>
    internal enum ResourceFailureClass
    {
        Unknown,
        ContainerRuntimeDown,
        ImagePullFailure,
        PortInUse,
        OutOfMemory,
        NonZeroExit,
        CrashedNoCode,
        HealthCheckFailing,
        NeverStarted,
    }

    /// <summary>A single resource's classified failure state plus a one-line detail and hint.</summary>
    internal readonly record struct ResourceDiagnosis(
        string Name,
        string State,
        int? ExitCode,
        ResourceFailureClass Class,
        string? Detail,
        string? Hint);

    /// <summary>One recorded state/health transition in the startup timeline.</summary>
    internal readonly record struct StateTransition(
        TimeSpan Elapsed,
        string Resource,
        string From,
        string To,
        string? HealthDelta);

    private static bool StateEquals(string state, string known)
        => string.Equals(state, known, StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalState(string state)
        => StateEquals(state, KnownResourceStates.FailedToStart)
        || StateEquals(state, KnownResourceStates.Exited)
        || StateEquals(state, KnownResourceStates.Finished);

    private static bool IsPendingState(string state)
        => StateEquals(state, KnownResourceStates.Starting)
        || StateEquals(state, KnownResourceStates.Waiting)
        || StateEquals(state, KnownResourceStates.NotStarted)
        || StateEquals(state, KnownResourceStates.Building);

    /// <summary>Scans recent log lines for high-confidence failure signatures. Heuristic; a miss
    /// just falls through to state-based classification.</summary>
    internal static ResourceFailureClass? ScanLogSignatures(IReadOnlyList<string>? lines)
    {
        if (lines is null)
        {
            return null;
        }

        const StringComparison Ci = StringComparison.OrdinalIgnoreCase;

        foreach (var line in lines)
        {
            if (line.Contains("cannot connect to the docker daemon", Ci)
                || line.Contains("is the docker daemon running", Ci)
                || line.Contains("error during connect", Ci))
            {
                return ResourceFailureClass.ContainerRuntimeDown;
            }

            if (line.Contains("address already in use", Ci)
                || line.Contains("port is already allocated", Ci)
                || line.Contains("bind: address already in use", Ci))
            {
                return ResourceFailureClass.PortInUse;
            }

            if (line.Contains("pull access denied", Ci)
                || line.Contains("manifest unknown", Ci)
                || line.Contains("repository does not exist", Ci)
                || line.Contains("not found: manifest", Ci)
                || line.Contains("no such image", Ci))
            {
                return ResourceFailureClass.ImagePullFailure;
            }
        }

        return null;
    }

    /// <summary>Pure classification used by <see cref="DiagnoseResource"/>; separated for unit testing.</summary>
    internal static ResourceFailureClass Classify(
        string state, int? exitCode, bool runningButUnhealthy, IReadOnlyList<string>? logLines)
    {
        if (exitCode == 137)
        {
            return ResourceFailureClass.OutOfMemory;
        }

        if (ScanLogSignatures(logLines) is { } signature)
        {
            return signature;
        }

        if (StateEquals(state, KnownResourceStates.RuntimeUnhealthy))
        {
            return ResourceFailureClass.ContainerRuntimeDown;
        }

        if (runningButUnhealthy)
        {
            return ResourceFailureClass.HealthCheckFailing;
        }

        if (IsTerminalState(state))
        {
            return exitCode is int code && code != 0
                ? ResourceFailureClass.NonZeroExit
                : ResourceFailureClass.CrashedNoCode;
        }

        if (IsPendingState(state))
        {
            return ResourceFailureClass.NeverStarted;
        }

        return ResourceFailureClass.Unknown;
    }

    /// <summary>Maps a failure class to a single actionable hint, or <c>null</c> when nothing specific applies.</summary>
    internal static string? HintFor(ResourceFailureClass cls) => cls switch
    {
        ResourceFailureClass.ContainerRuntimeDown =>
            "The container runtime is not reachable. Is Docker/Podman running? Start it (e.g. Docker Desktop, or `sudo systemctl start docker`) and retry.",
        ResourceFailureClass.ImagePullFailure =>
            "The container image could not be pulled. Check the image name/tag, registry authentication (`docker login`), and network access.",
        ResourceFailureClass.PortInUse =>
            "A required port is already in use. Stop the process holding it, or avoid pinning host ports in tests so Aspire can pick a free one.",
        ResourceFailureClass.OutOfMemory =>
            "Exit code 137 means the container was killed (OOM / SIGKILL). Increase the container memory limit or the resources allocated to Docker.",
        ResourceFailureClass.NonZeroExit =>
            "The process exited with a non-zero code on startup. The logs usually show the cause (bad config, missing connection string, failed migration).",
        ResourceFailureClass.HealthCheckFailing =>
            "The container is Running but its health check never passed. It started but isn't ready yet — check the health endpoint, slow startup, or raise ResourceTimeout.",
        ResourceFailureClass.NeverStarted =>
            "The resource never reached Running. It is still booting or blocked on a dependency — check the logs and the dependency chain.",
        ResourceFailureClass.CrashedNoCode =>
            "The resource entered a failed state without an exit code. Check the logs and the container runtime.",
        _ => null,
    };

    /// <summary>One-line human description of a resource's failure state for the concise exception message.</summary>
    internal static string DescribeState(in ResourceDiagnosis d) => d.Class switch
    {
        ResourceFailureClass.OutOfMemory => $"{d.State}, exit code {d.ExitCode} (OOM / SIGKILL)",
        ResourceFailureClass.NonZeroExit => $"{d.State}, exit code {d.ExitCode}",
        ResourceFailureClass.CrashedNoCode => $"{d.State} (no exit code)",
        ResourceFailureClass.HealthCheckFailing => d.Detail is { Length: > 0 } health
            ? $"Running but not Healthy ({health})"
            : "Running but not Healthy",
        ResourceFailureClass.NeverStarted => d.Detail is { Length: > 0 } dependency
            ? $"{d.State}, waiting on {dependency}"
            : $"still {d.State} (never reached Running)",
        ResourceFailureClass.ContainerRuntimeDown => $"{d.State} — container runtime unreachable",
        ResourceFailureClass.ImagePullFailure => $"{d.State} — image pull failed",
        ResourceFailureClass.PortInUse => $"{d.State} — port already in use",
        _ => d.State,
    };

    /// <summary>Reads a resource's current Aspire snapshot and classifies why it failed or is not ready.</summary>
    private ResourceDiagnosis DiagnoseResource(
        ResourceNotificationService notificationService,
        string name,
        IReadOnlyList<string>? recentLogLines)
    {
        var state = "unknown";
        int? exitCode = null;
        var runningButUnhealthy = false;
        string? detail = null;

        if (notificationService.TryGetCurrentState(name, out var ev) && ev is not null)
        {
            var snapshot = ev.Snapshot;
            state = snapshot.State?.Text ?? "unknown";
            exitCode = snapshot.ExitCode;

            if (StateEquals(state, KnownResourceStates.Running)
                && FirstFailingHealthReport(snapshot.HealthReports) is { } report)
            {
                runningButUnhealthy = true;
                detail = SummarizeHealthReport(report);
            }
            else if (StateEquals(state, KnownResourceStates.Waiting))
            {
                detail = SummarizeDependencies(notificationService, ev.Resource);
            }
        }

        var cls = Classify(state, exitCode, runningButUnhealthy, recentLogLines);
        return new ResourceDiagnosis(name, state, exitCode, cls, detail, HintFor(cls));
    }

    private static HealthReportSnapshot? FirstFailingHealthReport(ImmutableArray<HealthReportSnapshot> reports)
    {
        if (reports.IsDefaultOrEmpty)
        {
            return null;
        }

        foreach (var report in reports)
        {
            if (report.Status != HealthStatus.Healthy)
            {
                return report;
            }
        }

        return null;
    }

    private static string SummarizeHealthReport(HealthReportSnapshot report)
    {
        var status = report.Status?.ToString() ?? "Pending";
        var reason = FirstLine(report.Description) ?? FirstLine(report.ExceptionText);
        return reason is null ? $"{report.Name} {status}" : $"{report.Name} {status}: {reason}";
    }

    private static string? SummarizeDependencies(ResourceNotificationService notificationService, IResource resource)
    {
        if (!resource.TryGetAnnotationsOfType<WaitAnnotation>(out var waits))
        {
            return null;
        }

        foreach (var wait in waits)
        {
            var depName = wait.Resource.Name;
            if (notificationService.TryGetCurrentState(depName, out var dep) && dep is not null)
            {
                var depState = dep.Snapshot.State?.Text ?? "unknown";
                return dep.Snapshot.ExitCode is int code
                    ? $"'{depName}' ({depState}, exit code {code})"
                    : $"'{depName}' ({depState})";
            }

            return $"'{depName}'";
        }

        return null;
    }

    private static string? FirstLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var idx = text.IndexOfAny(['\r', '\n']);
        var line = (idx >= 0 ? text[..idx] : text).Trim();
        return line.Length == 0 ? null : line;
    }

    private string FormatTimeline() => FormatTimeline(_timeline.ToArray());

    /// <summary>Renders the recorded transitions as a relative-time timeline; "" when there's nothing useful.</summary>
    internal static string FormatTimeline(StateTransition[] transitions)
    {
        if (transitions.Length < 2)
        {
            return string.Empty;
        }

        var origin = transitions[0].Elapsed;
        var sb = new StringBuilder();

        foreach (var t in transitions)
        {
            var seconds = (t.Elapsed - origin).TotalSeconds;
            sb.Append($"  +{seconds,5:0.0}s  [{t.Resource}] {t.From} -> {t.To}");
            if (t.HealthDelta is { Length: > 0 })
            {
                sb.Append($"   ({t.HealthDelta})");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a concise diagnosis for the exception message, writes the full timeline + untruncated
    /// resource logs to an attached artifact, and returns the concise text plus an artifact pointer.
    /// </summary>
    private async Task<string> BuildDiagnosticsAndAttachAsync(
        DistributedApplication app,
        ResourceNotificationService notificationService,
        string headline,
        IReadOnlyList<string> problemResources,
        IReadOnlyCollection<string>? readyResources = null)
    {
        var concise = new StringBuilder().Append(headline);
        var full = new StringBuilder().AppendLine(headline);

        foreach (var name in problemResources)
        {
            var logLines = await CollectResourceLogLinesAsync(app, name);
            var diagnosis = DiagnoseResource(notificationService, name, logLines);

            concise.AppendLine().Append($"  {name}: {DescribeState(diagnosis)}");
            if (diagnosis.Hint is not null)
            {
                concise.AppendLine().Append($"     Hint: {diagnosis.Hint}");
            }

            AppendResourceDiagnosticBlock(full, notificationService, name, diagnosis, logLines);
        }

        if (readyResources is { Count: > 0 })
        {
            var readyList = string.Join(", ", readyResources.Select(n => $"'{n}'"));
            concise.AppendLine().Append($"  Ready: [{readyList}]");
            full.AppendLine().AppendLine($"Ready resources: [{readyList}]");
        }

        var timeline = FormatTimeline();
        if (timeline.Length > 0)
        {
            full.AppendLine().AppendLine("--- startup timeline ---").Append(timeline);
        }

        var pointer = await TryWriteDiagnosticsArtifactAsync(full.ToString());
        if (pointer is not null)
        {
            concise.AppendLine().AppendLine().Append(pointer);
        }

        return concise.ToString();
    }

    private void AppendResourceDiagnosticBlock(
        StringBuilder sb,
        ResourceNotificationService notificationService,
        string name,
        in ResourceDiagnosis diagnosis,
        IReadOnlyList<string> logLines)
    {
        sb.AppendLine().AppendLine($"--- {name} diagnostics ---");
        sb.AppendLine($"  State:      {diagnosis.State}");
        if (diagnosis.ExitCode is int code)
        {
            sb.AppendLine($"  Exit code:  {code}");
        }
        if (diagnosis.Hint is not null)
        {
            sb.AppendLine($"  Hint:       {diagnosis.Hint}");
        }

        if (notificationService.TryGetCurrentState(name, out var ev) && ev is not null)
        {
            AppendHealthReports(sb, ev.Snapshot.HealthReports);
            AppendDependencyChain(sb, notificationService, ev.Resource);
        }

        sb.AppendLine($"  Logs ({(logLines.Count == 0 ? "none" : logLines.Count + " line(s)")}):");
        if (logLines.Count == 0)
        {
            sb.AppendLine("    (no logs available)");
        }
        else
        {
            foreach (var line in logLines)
            {
                sb.Append("    ").AppendLine(line);
            }
        }
    }

    private static void AppendHealthReports(StringBuilder sb, ImmutableArray<HealthReportSnapshot> reports)
    {
        if (reports.IsDefaultOrEmpty)
        {
            return;
        }

        var failing = reports.Where(r => r.Status != HealthStatus.Healthy).ToList();
        if (failing.Count == 0)
        {
            return;
        }

        sb.AppendLine("  Health checks:");
        foreach (var report in failing)
        {
            var status = report.Status?.ToString() ?? "Pending";
            sb.Append($"    [{report.Name}] {status}");
            if (FirstLine(report.Description) is { } description)
            {
                sb.Append($" — {description}");
            }
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(report.ExceptionText))
            {
                foreach (var line in report.ExceptionText.Split('\n'))
                {
                    sb.Append("        ").AppendLine(line.TrimEnd('\r'));
                }
            }
        }
    }

    private static void AppendDependencyChain(StringBuilder sb, ResourceNotificationService notificationService, IResource resource)
    {
        if (!resource.TryGetAnnotationsOfType<WaitAnnotation>(out var waits))
        {
            return;
        }

        var list = waits.ToList();
        if (list.Count == 0)
        {
            return;
        }

        sb.AppendLine("  Waiting on dependencies:");
        foreach (var wait in list)
        {
            var depName = wait.Resource.Name;
            string description;
            if (notificationService.TryGetCurrentState(depName, out var dep) && dep is not null)
            {
                var depState = dep.Snapshot.State?.Text ?? "unknown";
                description = dep.Snapshot.ExitCode is int code ? $"{depState}, exit code {code}" : depState;
            }
            else
            {
                description = "unknown";
            }

            sb.AppendLine($"    - {depName} [{wait.WaitType}] -> {description}");
        }
    }

    private async Task<string?> TryWriteDiagnosticsArtifactAsync(string content)
    {
        try
        {
            var directory = TestContext.OutputDirectory ?? Path.GetTempPath();
            var fileName = $"aspire-diagnostics-{typeof(TAppHost).Name}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log";
            var path = Path.Combine(directory, fileName);

            await File.WriteAllTextAsync(path, content);

            var artifact = new Artifact
            {
                File = new FileInfo(path),
                DisplayName = $"Aspire startup diagnostics ({typeof(TAppHost).Name})",
                Description = "Full Aspire resource startup timeline and logs.",
            };

            if (TestContext.Current is { } testContext)
            {
                testContext.Output.AttachArtifact(artifact);
            }
            else if (TestSessionContext.Current is { } sessionContext)
            {
                sessionContext.AddArtifact(artifact);
            }

            return $"Full startup diagnostics (timeline + untruncated logs) written to: {path}";
        }
        catch
        {
            // Diagnostics must never mask the underlying startup failure.
            return null;
        }
    }

    private sealed class ResourceLogWatcher(CancellationTokenSource cts) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            cts.Cancel();
            cts.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
