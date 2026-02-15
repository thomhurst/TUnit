using System.Collections.Concurrent;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;
using TUnit.Core.Interfaces;

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

    /// <summary>
    /// The running Aspire distributed application.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="InitializeAsync"/> completes.</exception>
    public DistributedApplication App => _app ?? throw new InvalidOperationException(
        "App not initialized. Ensure InitializeAsync has completed.");

    /// <summary>
    /// Creates an <see cref="HttpClient"/> for the named resource.
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
                        testContext.Output.WriteLine($"[{resourceName}] {line}");
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

    /// <summary>
    /// Override to customize the builder before building the application.
    /// </summary>
    /// <param name="builder">The distributed application testing builder.</param>
    protected virtual void ConfigureBuilder(IDistributedApplicationTestingBuilder builder) { }

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
                var names = model.Resources.Select(r => r.Name).ToList();
                await WaitForResourcesWithFailFastAsync(app, notificationService, names, waitForHealthy: true, cancellationToken);
                break;
            }

            case ResourceWaitBehavior.AllRunning:
            {
                var model = app.Services.GetRequiredService<DistributedApplicationModel>();
                var names = model.Resources.Select(r => r.Name).ToList();
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

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TAppHost>();
        ConfigureBuilder(builder);

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        using var cts = new CancellationTokenSource(ResourceTimeout);

        try
        {
            await WaitForResourcesAsync(_app, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Fallback for custom WaitForResourcesAsync overrides that don't use the
            // default fail-fast helper. The default implementation throws its own
            // TimeoutException with resource logs before this catch is reached.
            var model = _app.Services.GetRequiredService<DistributedApplicationModel>();
            var resourceNames = string.Join(", ", model.Resources.Select(r => $"'{r.Name}'"));

            throw new TimeoutException(
                $"Timed out after {ResourceTimeout.TotalSeconds:0}s waiting for Aspire resources to be ready. " +
                $"Resources: [{resourceNames}]. " +
                $"Wait behavior: {WaitBehavior}. " +
                $"Consider increasing ResourceTimeout, checking resource health, or using WatchResourceLogs() to diagnose startup issues.");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Waits for all named resources to reach the desired state, while simultaneously
    /// watching for any resource entering FailedToStart. If a resource fails, throws
    /// immediately with its recent logs. On timeout, reports which resources are still
    /// pending and includes their logs.
    /// </summary>
    private static async Task WaitForResourcesWithFailFastAsync(
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

        // Track which resources have become ready (for timeout reporting)
        var readyResources = new ConcurrentBag<string>();

        // Linked CTS lets us cancel the failure watchers once all resources are ready
        using var failureCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Success path: wait for all resources to reach the desired state
        var readyTask = Task.WhenAll(resourceNames.Select(async name =>
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
        }));

        // Fail-fast path: complete as soon as ANY resource enters FailedToStart
        var failureTasks = resourceNames.Select(async name =>
        {
            await notificationService.WaitForResourceAsync(name, KnownResourceStates.FailedToStart, failureCts.Token);
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
                var logs = await CollectResourceLogsAsync(app, failedName);

                throw new InvalidOperationException(
                    $"Resource '{failedName}' failed to start." +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"--- {failedName} logs ---{Environment.NewLine}" +
                    logs);
            }

            // All resources are ready - cancel the failure watchers
            failureCts.Cancel();
            await readyTask;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Timeout - report which resources are ready vs. still pending, with logs
            failureCts.Cancel();

            var readySet = new HashSet<string>(readyResources);
            var pending = resourceNames.Where(n => !readySet.Contains(n)).ToList();

            var sb = new StringBuilder();
            sb.Append("Resources not ready: [");
            sb.Append(string.Join(", ", pending.Select(n => $"'{n}'")));
            sb.Append(']');

            if (readySet.Count > 0)
            {
                sb.Append(". Resources ready: [");
                sb.Append(string.Join(", ", readySet.Select(n => $"'{n}'")));
                sb.Append(']');
            }

            foreach (var name in pending)
            {
                var logs = await CollectResourceLogsAsync(app, name);
                sb.AppendLine().AppendLine();
                sb.AppendLine($"--- {name} logs ---");
                sb.Append(logs);
            }

            throw new TimeoutException(sb.ToString());
        }
    }

    /// <summary>
    /// Collects recent log lines from a resource via the <see cref="ResourceLoggerService"/>.
    /// Returns the last <paramref name="maxLines"/> lines, or "(no logs available)" if none.
    /// Uses a short timeout to avoid hanging if the log service is unresponsive.
    /// </summary>
    private static async Task<string> CollectResourceLogsAsync(
        DistributedApplication app,
        string resourceName,
        int maxLines = 20)
    {
        var loggerService = app.Services.GetRequiredService<ResourceLoggerService>();
        var lines = new List<string>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            await foreach (var batch in loggerService.WatchAsync(resourceName)
                .WithCancellation(cts.Token))
            {
                foreach (var line in batch)
                {
                    lines.Add($"  {line}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected - we use a short timeout to collect buffered logs
        }

        if (lines.Count == 0)
        {
            return "  (no logs available)";
        }

        if (lines.Count > maxLines)
        {
            return $"  ... ({lines.Count - maxLines} earlier lines omitted){Environment.NewLine}" +
                   string.Join(Environment.NewLine, lines.Skip(lines.Count - maxLines));
        }

        return string.Join(Environment.NewLine, lines);
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
