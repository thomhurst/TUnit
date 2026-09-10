namespace TUnit.Aspire;

/// <summary>
/// Optional configuration for <see cref="AspireFixture{TAppHost}"/>, returned from the
/// fixture's <see cref="AspireFixture{TAppHost}.Options"/> hook.
/// </summary>
/// <remarks>
/// This is a small, additive options bag — the fixture's other knobs remain individual
/// virtual properties. Override <see cref="AspireFixture{TAppHost}.Options"/> to supply values:
/// <code>
/// protected override AspireFixtureOptions Options => new() { ForwardResourceLogs = true };
/// </code>
/// </remarks>
public sealed class AspireFixtureOptions
{
    /// <summary>
    /// When <c>true</c>, subscribes to each selected resource's console output (stdout and
    /// stderr) as soon as the application is built and forwards every line, prefixed with the
    /// resource name, into the captured output of the test that owns the fixture lifecycle.
    /// Default: <c>false</c> (opt-in).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscription starts before the application is started, so logs from a resource that
    /// crashes during boot — before its OpenTelemetry exporter flushes — are still captured.
    /// This is the common Aspire-test failure ("a resource won't come up") that OTel misses.
    /// </para>
    /// <para>
    /// The lines attach to the fixture-owner test (the test whose execution triggered
    /// initialization) for the whole life of the subscription. On a shared (session/class)
    /// fixture that is whichever test triggered initialization; forwarding is not
    /// per-request-correlated — for that, rely on the OTLP receiver
    /// (<c>EnableTelemetryCollection</c>). When initialization runs outside any test, lines
    /// fall back to the standard error stream for CI visibility.
    /// </para>
    /// <para>
    /// Enabling this also makes a failed fixture initialization self-document: on a startup failure
    /// the fixture emits a consolidated resource-diagnostics block (state, decoded exit code, and
    /// recent logs — the same content as <see cref="AspireFixture{TAppHost}.DumpResourceDiagnosticsAsync"/>)
    /// to the progress log, covering even the failure paths that don't build their own diagnostics.
    /// </para>
    /// </remarks>
    public bool ForwardResourceLogs { get; set; }

    /// <summary>
    /// The resources whose logs to forward when <see cref="ForwardResourceLogs"/> is on, and to
    /// retain when <see cref="RetainResourceLogs"/> is on. When <c>null</c> or empty (the default),
    /// every waited-on resource is selected (those chosen by <c>ShouldWaitForResource</c> —
    /// containers, projects, executables). When names are supplied, only those resources are
    /// selected, bypassing that filter.
    /// </summary>
    public IReadOnlyCollection<string>? ResourceLogNames { get; set; }

    /// <summary>
    /// When <c>true</c>, retains a bounded rolling buffer of each selected resource's most recent
    /// console output in memory (subscribed as soon as the application is built, before it starts).
    /// This makes on-demand and post-mortem retrieval via
    /// <see cref="AspireFixture{TAppHost}.GetResourceLogsAsync"/> and
    /// <see cref="AspireFixture{TAppHost}.DumpResourceDiagnosticsAsync"/> reliable even for a
    /// resource that has already exited — avoiding the "(no logs available)" race you get when
    /// fetching logs via <c>ResourceLoggerService.WatchAsync</c> only after the fact. Default:
    /// <c>false</c> (opt-in). When off, those helpers fall back to a best-effort live backlog read.
    /// </summary>
    /// <remarks>
    /// The buffer is bounded to <see cref="ResourceLogBufferCapacity"/> lines per resource. It is
    /// scoped by <see cref="ResourceLogNames"/> just like forwarding, and shares the same background
    /// pump when both are enabled.
    /// </remarks>
    public bool RetainResourceLogs { get; set; }

    /// <summary>
    /// Maximum number of most-recent log lines retained per resource when
    /// <see cref="RetainResourceLogs"/> is on. Bounds memory on long or noisy runs. Values below 1
    /// are clamped to 1. Default: 500.
    /// </summary>
    public int ResourceLogBufferCapacity { get; set; } = 500;
}
