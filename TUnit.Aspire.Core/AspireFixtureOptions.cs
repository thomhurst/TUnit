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
    /// </remarks>
    public bool ForwardResourceLogs { get; set; }

    /// <summary>
    /// The resources whose logs to forward when <see cref="ForwardResourceLogs"/> is on.
    /// When <c>null</c> or empty (the default), every waited-on resource is forwarded (those
    /// selected by <c>ShouldWaitForResource</c> — containers, projects, executables). When
    /// names are supplied, only those resources are forwarded, bypassing that filter.
    /// </summary>
    public IReadOnlyCollection<string>? ResourceLogNames { get; set; }
}
