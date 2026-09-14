using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class BrowserTest : PlaywrightTest
{
    public BrowserTest() : this(TUnitPlaywrightSettings.Default.DefaultBrowserTypeLaunchOptions ?? new BrowserTypeLaunchOptions())
    {
    }

    public BrowserTest(BrowserTypeLaunchOptions options)
    {
        _options = options;
    }

    public IBrowser Browser { get; internal set; } = null!;

    /// <summary>
    /// Seeds each <see cref="IBrowserContext"/> created via <see cref="NewContext"/>
    /// with W3C <c>traceparent</c>/<c>baggage</c> headers from the current test's
    /// <see cref="System.Diagnostics.Activity"/>. Override to <c>false</c> to avoid
    /// leaking trace ids to third-party domains the page contacts.
    /// </summary>
    /// <remarks>
    /// Has no effect on <c>netstandard2.0</c> targets — the engine's Activity plumbing
    /// is .NET-only.
    /// </remarks>
    public virtual bool PropagateTraceContext => true;

    private readonly List<IBrowserContext> _contexts = [];
    // Tracks every page ever opened in a context this test created, including ones closed
    // before teardown - IBrowserContext.Pages drops a page as soon as it closes, which would
    // otherwise lose the video of a page closed early to flush its recording.
    private readonly List<IPage> _pages = [];
    private readonly Lock _contextsLock = new();
    private readonly BrowserTypeLaunchOptions _options;

    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
    {
        options = PlaywrightTelemetryHeaders.Merge(options, PropagateTraceContext);
        var context = await Browser.NewContextAsync(options).ConfigureAwait(false);

        context.Page += OnContextPage;

        lock (_contextsLock)
        {
            _contexts.Add(context);
        }

        return context;
    }

    private void OnContextPage(object? sender, IPage page)
    {
        lock (_contextsLock)
        {
            _pages.Add(page);
        }
    }

    [Before(HookType.Test, "", 0)]
    public async Task BrowserSetup()
    {
        if (BrowserType == null)
        {
            throw new InvalidOperationException($"BrowserType is not initialized. This may indicate that {nameof(PlaywrightTest)}.{nameof(Playwright)} is not initialized or {nameof(PlaywrightTest)}.{nameof(PlaywrightSetup)} did not execute properly.");
        }

        var service = await BrowserService.Register(this, BrowserType, _options).ConfigureAwait(false);
        Browser = service.Browser;
    }

    [After(HookType.Test, "", 0)]
    public async Task BrowserTearDown(TestContext testContext)
    {
        List<IBrowserContext> contextsSnapshot;
        List<IPage> pagesSnapshot;

        lock (_contextsLock)
        {
            contextsSnapshot = [.. _contexts];
            _contexts.Clear();
            pagesSnapshot = [.. _pages];
            _pages.Clear();
        }

        foreach (var context in contextsSnapshot)
        {
            context.Page -= OnContextPage;
        }

        // IVideo.PathAsync() only resolves its final path once the recording is flushed to
        // disk, which Playwright guarantees once the owning page (and so its context) has
        // closed - so every video reference has to be grabbed before closing, then read
        // back afterwards. Capturing them here, in the hook that actually closes every
        // context this test opened, means the rename can happen while this test's own
        // TestContext is still the one executing - late enough that the file is finished,
        // but before this test reports its result, so the renamed file attaches to this
        // test's own result rather than becoming a run-wide artifact.
        //
        // Pages are read from the tracked list rather than IBrowserContext.Pages, which
        // drops a page - and its video - the moment the page closes; closing a page before
        // teardown is an established way to flush its recording early.
        var videos = new List<IVideo>();

        foreach (var page in pagesSnapshot)
        {
            if (page.Video is { } video)
            {
                videos.Add(video);
            }
        }

        List<Exception>? exceptions = null;

        foreach (var context in contextsSnapshot)
        {
            try
            {
                await context.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        Browser = null!;

        if (videos.Count > 0)
        {
            await RenameAndAttachVideosAsync(testContext, videos).ConfigureAwait(false);
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("One or more browser contexts failed to close.", exceptions);
        }
    }

    // Playwright names its recordings page@<hash>.webm, which tells you nothing about which
    // test produced which video once CI has uploaded a dozen of them, and the name can't be
    // set through RecordVideoDir - so rename each one to reflect the test that recorded it.
    private static async Task RenameAndAttachVideosAsync(TestContext testContext, List<IVideo> videos)
    {
        // A retried test records once per attempt; number them so the flaky-test videos
        // line up with the attempts shown in the run report instead of overwriting.
        var attempt = testContext.Execution.CurrentRetryAttempt;
        var baseName = SanitizeForFileName(testContext.Metadata.TestName) +
                       (attempt > 0 ? $"-attempt{attempt + 1}" : string.Empty);

        for (var i = 0; i < videos.Count; i++)
        {
            try
            {
                var sourcePath = await videos[i].PathAsync().ConfigureAwait(false);

                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(sourcePath)!;
                // A test that opens more than one page records more than one video.
                var suffix = videos.Count > 1 ? $"-{i + 1}" : string.Empty;
                var target = Path.Combine(directory, $"{baseName}{suffix}.webm");

                // Last-resort de-duplication in case a target name is somehow already taken.
                for (var n = 2; File.Exists(target); n++)
                {
                    target = Path.Combine(directory, $"{baseName}{suffix}-{n}.webm");
                }

                File.Move(sourcePath, target);

                testContext.Output.AttachArtifact(target, Path.GetFileName(target), "Playwright video recording");
            }
            catch (Exception renameFailure)
            {
                // A recording we couldn't rename is still a usable recording - never fail
                // a run (or hide the real result) over cosmetic artifact naming.
                Console.WriteLine($"Could not rename video for {testContext.Metadata.TestName}: {renameFailure.Message}");
            }
        }
    }

    private static string SanitizeForFileName(string value) =>
        string.Concat(value.Split(Path.GetInvalidFileNameChars())).Replace(' ', '-');
}
