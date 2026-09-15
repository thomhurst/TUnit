using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

/// <summary>Owns recording and completion for one browser context and test attempt.</summary>
internal sealed class PlaywrightVideoRecorder
{
    private readonly IBrowserContext _context;
    private readonly TestContext _owner;
    private readonly int _attempt;
    private readonly Lock _lock = new();
    private readonly List<IVideo> _videos = [];
    private bool _acceptPages = true;
    private Task? _completion;

    internal PlaywrightVideoRecorder(IBrowserContext context, TestContext owner)
    {
        _context = context;
        _owner = owner;
        _attempt = owner.Execution.CurrentRetryAttempt;
        context.Page += OnPage;
    }

    private void OnPage(object? sender, IPage page)
    {
        lock (_lock)
        {
            if (_acceptPages && page.Video is { } video)
            {
                _videos.Add(video);
            }
        }
    }

    internal Task CloseAsync()
    {
        lock (_lock)
        {
            return _completion ??= CompleteAsync();
        }
    }

    private async Task CompleteAsync()
    {
        try
        {
            // Keep tracking through closure to include late popups and flush recordings.
            await _context.CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            List<IVideo> videos;
            lock (_lock)
            {
                _acceptPages = false;
                _context.Page -= OnPage;
                videos = [.. _videos];
                _videos.Clear();
            }

            await RenameAndAttachVideosAsync(videos).ConfigureAwait(false);
        }
    }

    // Playwright names its recordings page@<hash>.webm, which tells you nothing about which
    // test produced which video once CI has uploaded a dozen of them, and the name can't be
    // set through RecordVideoDir - so rename each one to reflect the test that recorded it.
    private async Task RenameAndAttachVideosAsync(List<IVideo> videos)
    {
        // A retried test records once per attempt; number them so the flaky-test videos
        // line up with the attempts shown in the run report instead of overwriting.
        var testContext = _owner;
        var attempt = _attempt;
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
