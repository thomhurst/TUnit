using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

/// <summary>
/// Captures a full-page screenshot of each open page and a Playwright trace when a test does not
/// pass, and attaches them to the test result. Applies to the browser contexts that
/// <see cref="ContextTest"/>, <see cref="PageTest"/> and per-test <see cref="ContextFixture"/> and
/// <see cref="PageFixture"/> instances create.
/// </summary>
/// <param name="path">
/// The directory that screenshots and traces are written to, resolved relative to the test
/// application's working directory unless given as an absolute path. Defaults to
/// <c>"playwright-artifacts"</c>.
/// </param>
/// <remarks>
/// <para>
/// Tracing starts when the browser context is created. When the test passes or is skipped, the
/// trace is discarded and nothing is written. Otherwise each file is named
/// <c>&lt;test id hash&gt;_&lt;display name&gt;</c>, with an <c>-attemptN</c> suffix for retries, so
/// parallel tests and <c>[Arguments]</c> variants of one method never share a file.
/// </para>
/// <para>
/// The decision uses the result after every <c>[After(Test)]</c> and <c>[AfterEvery(Test)]</c> hook,
/// so a failing teardown hook is captured. A test-end event receiver that fails after capture ran
/// still fails the test, but its artifacts were already discarded.
/// </para>
/// <para>
/// Apply the attribute to a method, a class or an assembly. The closest one applies.
/// Fixtures shared between tests are not captured: they have no single test that owns them.
/// When capture applies, context and page fixtures are created again for each retry attempt.
/// Fixture capture needs .NET 8 or later; the <c>netstandard2.0</c> build cannot tell whether a
/// fixture is shared, so it captures only <see cref="ContextTest"/> and <see cref="PageTest"/>.
/// </para>
/// </remarks>
#pragma warning disable TUnit0028 // This framework attribute defines its own targets; it does not override a user-facing attribute.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
#pragma warning restore TUnit0028
public class CaptureOnFailureAttribute(string path = "playwright-artifacts")
    : TUnitAttribute, ITestDiscoveryEventReceiver, ITestEndEventReceiver, IScopedAttribute
{
    internal const string StateBagKey = "TUnit.Playwright.CaptureOnFailureAttribute";

    /// <summary>
    /// The directory that screenshots and traces are written to. Defaults to <c>"playwright-artifacts"</c>.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Whether to take a full-page screenshot of each open page when the test does not pass.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool Screenshot { get; set; } = true;

    /// <summary>
    /// Whether to record a Playwright trace and save it when the test does not pass.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool Trace { get; set; } = true;

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public Type ScopeType => typeof(CaptureOnFailureAttribute);

    /// <inheritdoc />
    public ValueTask OnTestDiscovered(DiscoveredTestContext discoveredTestContext)
    {
        if (Screenshot || Trace)
        {
            discoveredTestContext.TestContext.StateBag[StateBagKey] = this;
        }

        return default;
    }

    /// <inheritdoc />
    public ValueTask OnTestEnd(TestContext context) => PlaywrightRecordingScope.CompleteAsync(context);
}
