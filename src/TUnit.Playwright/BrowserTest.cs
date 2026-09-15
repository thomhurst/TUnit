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
    // Retain videos from pages closed before teardown: IBrowserContext.Pages drops
    // each page as soon as it closes. Allocate only for contexts with recording enabled.
    private List<IVideo>? _videos;
    private readonly Lock _contextsLock = new();
    private readonly BrowserTypeLaunchOptions _options;

    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
    {
        options = PlaywrightTelemetryHeaders.Merge(options, PropagateTraceContext);
        var context = await Browser.NewContextAsync(options).ConfigureAwait(false);

        lock (_contextsLock)
        {
            _contexts.Add(context);
            if (!string.IsNullOrEmpty(options.RecordVideoDir))
            {
                _videos ??= [];
                context.Page += OnContextPage;
            }
        }

        return context;
    }

    private void OnContextPage(object? sender, IPage page)
    {
        lock (_contextsLock)
        {
            // An event already in flight can arrive after teardown unsubscribes.
            if (sender is IBrowserContext context && _contexts.Contains(context) && page.Video is { } video)
            {
                _videos?.Add(video);
            }
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
        lock (_contextsLock)
        {
            contextsSnapshot = [.. _contexts];
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

        // Keep tracking until all contexts have closed so popups opened during teardown
        // are included. Video paths are read only after context closure flushes recordings.
        List<IVideo>? videos;
        lock (_contextsLock)
        {
            foreach (var context in contextsSnapshot)
            {
                context.Page -= OnContextPage;
            }

            _contexts.Clear();
            videos = _videos;
            _videos = null;
        }

        Browser = null!;

        if (videos is { Count: > 0 })
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

                // File.Move throws if the target already exists, so retry with an
                // incremented counter on that specific failure instead of checking
                // existence beforehand - a concurrently running test could create the
                // target between such a check and the move, and a pre-check alone
                // wouldn't catch that race.
                for (var n = 2; ; n++)
                {
                    try
                    {
                        File.Move(sourcePath, target);
                        break;
                    }
                    catch (IOException) when (File.Exists(target) && n < 1000)
                    {
                        target = Path.Combine(directory, $"{baseName}{suffix}-{n}.webm");
                    }
                }

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

    private static string SanitizeForFileName(string value)
    {
        var characters = value.ToCharArray();
        var invalidCharacters = Path.GetInvalidFileNameChars();
        for (var i = 0; i < characters.Length; i++)
        {
            if (Array.IndexOf(invalidCharacters, characters[i]) >= 0)
            {
                characters[i] = '_';
            }
            else if (characters[i] == ' ')
            {
                characters[i] = '-';
            }
        }

        return characters.Length == 0 ? "test" : new string(characters);
    }
}
