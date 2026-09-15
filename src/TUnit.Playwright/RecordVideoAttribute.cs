using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

/// <summary>
/// Enables Playwright video recording for the browser context created by <see cref="ContextTest"/>
/// or by per-test <see cref="ContextFixture"/> and <see cref="PageFixture"/> instances.
/// </summary>
/// <param name="path">
/// The directory that recorded videos are written to, resolved relative to the test
/// application's working directory unless given as an absolute path. Defaults to
/// <c>"playwright-artifacts"</c>.
/// </param>
/// <param name="width">The viewport width used for the recording, in pixels. Defaults to <c>1280</c>.</param>
/// <param name="height">The viewport height used for the recording, in pixels. Defaults to <c>1400</c>.</param>
/// <remarks>
/// The recorded video is saved under <see cref="Path"/> once the browser context is closed.
/// Recording fixtures create fresh contexts and pages for each retry attempt.
/// Context and page fixtures must not be shared between tests; the browser may be shared.
/// </remarks>
#pragma warning disable TUnit0028 // This framework attribute defines its own targets; it does not override a user-facing attribute.
[AttributeUsage(AttributeTargets.Method)]
#pragma warning restore TUnit0028
public class RecordVideoAttribute(string path = "playwright-artifacts", int width = 1280, int height = 1400)
    : TUnitAttribute, ITestDiscoveryEventReceiver, ITestEndEventReceiver
{
    internal const string StateBagKey = "TUnit.Playwright.RecordVideoAttribute";

    /// <summary>
    /// The directory that recorded videos are written to. Defaults to <c>"playwright-artifacts"</c>.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// The viewport width used for the recording, in pixels. Defaults to <c>1280</c>.
    /// </summary>
    public int Width { get; } = width;

    /// <summary>
    /// The viewport height used for the recording, in pixels. Defaults to <c>1400</c>.
    /// </summary>
    public int Height { get; } = height;

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public ValueTask OnTestDiscovered(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.TestContext.StateBag[StateBagKey] = this;
        return default;
    }

    /// <inheritdoc />
    public ValueTask OnTestEnd(TestContext context) => PlaywrightRecordingScope.CompleteAsync(context);
}
