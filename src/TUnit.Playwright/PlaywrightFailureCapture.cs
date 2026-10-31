using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

/// <summary>Owns failure screenshots and tracing for one browser context and test attempt.</summary>
internal sealed class PlaywrightFailureCapture
{
    private readonly IBrowserContext _context;
    private readonly TestContext _owner;
    private readonly CaptureOnFailureAttribute _options;
    private readonly int _attempt;
    private readonly bool _tracing;
    private readonly Lock _lock = new();
    private Task? _completion;

    private PlaywrightFailureCapture(IBrowserContext context, TestContext owner, CaptureOnFailureAttribute options, bool tracing)
    {
        _context = context;
        _owner = owner;
        _options = options;
        _tracing = tracing;
        _attempt = owner.Execution.CurrentRetryAttempt;
    }

    internal static CaptureOnFailureAttribute? Options(TestContext? context) =>
        context is not null && context.StateBag.TryGetValue<CaptureOnFailureAttribute>(CaptureOnFailureAttribute.StateBagKey, out var options)
            ? options
            : null;

    /// <summary>
    /// Starts capture for <paramref name="context"/> when <paramref name="owner"/> asks for it.
    /// Throws when tracing cannot start; the caller owns closing the context.
    /// </summary>
    internal static async Task<PlaywrightFailureCapture?> StartAsync(IBrowserContext context, TestContext? owner)
    {
        if (owner is null || Options(owner) is not { } options)
        {
            return null;
        }

        if (options.Trace)
        {
            await context.Tracing.StartAsync(new TracingStartOptions
            {
                Title = owner.Metadata.DisplayName,
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            }).ConfigureAwait(false);
        }

        var capture = new PlaywrightFailureCapture(context, owner, options, options.Trace);
        PlaywrightRecordingScope.For(owner).RegisterCapture(capture);
        return capture;
    }

    /// <summary>Saves or discards the capture once. Never throws: capture must not hide the test result.</summary>
    internal Task CompleteAsync()
    {
        lock (_lock)
        {
            return _completion ??= CompleteCoreAsync();
        }
    }

    private async Task CompleteCoreAsync()
    {
        if (_owner.Execution.Result is not { State: not (TestState.Passed or TestState.Skipped) })
        {
            if (_tracing)
            {
                await TryAsync(() => _context.Tracing.StopAsync(), "discard trace").ConfigureAwait(false);
            }

            return;
        }

        var directory = System.IO.Path.GetFullPath(string.IsNullOrEmpty(_options.Path) ? "playwright-artifacts" : _options.Path);
        var baseName = PlaywrightArtifactNames.ForTest(_owner, _attempt);

        if (_options.Screenshot)
        {
            IReadOnlyList<IPage> pages = [];
            await TryAsync(() =>
            {
                pages = _context.Pages.ToArray();
                return Task.CompletedTask;
            }, "list pages").ConfigureAwait(false);

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                if (page.IsClosed)
                {
                    continue;
                }

                var suffix = pages.Count > 1 ? $"-page{i + 1}" : string.Empty;
                await SaveAsync(directory, baseName + suffix, ".png",
                    target => page.ScreenshotAsync(new PageScreenshotOptions { Path = target, FullPage = true }),
                    "Playwright failure screenshot", "take screenshot").ConfigureAwait(false);
            }
        }

        if (_tracing)
        {
            await SaveAsync(directory, baseName, ".trace.zip",
                target => _context.Tracing.StopAsync(new TracingStopOptions { Path = target }),
                "Playwright trace (open with 'playwright show-trace' or https://trace.playwright.dev)", "save trace").ConfigureAwait(false);
        }
    }

    private async Task SaveAsync(string directory, string name, string extension, Func<string, Task> save, string description, string operation)
    {
        string? target = null;
        try
        {
            target = PlaywrightArtifactNames.Reserve(directory, name, extension);
            await save(target).ConfigureAwait(false);
            if (new FileInfo(target).Length > 0)
            {
                _owner.Output.AttachArtifact(target, System.IO.Path.GetFileName(target), description);
                return;
            }
        }
        catch (Exception exception)
        {
            Report(operation, exception);
        }

        // Remove the placeholder that reserved the name when nothing was written to it.
        if (target is not null)
        {
            try
            {
                if (File.Exists(target) && new FileInfo(target).Length == 0)
                {
                    File.Delete(target);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private async Task TryAsync(Func<Task> action, string operation)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Report(operation, exception);
        }
    }

    // A missing artifact must never fail a run or replace the real failure.
    // The browser may already be gone, which is often why the test failed.
    private void Report(string operation, Exception exception) =>
        _owner.Output.ErrorOutput.WriteLine($"TUnit.Playwright could not {operation} for {_owner.Metadata.DisplayName}: {exception.Message}");
}
