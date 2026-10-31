using TUnit.Core;

namespace TUnit.Playwright;

internal sealed class PlaywrightRecordingScope
{
    private const string StateBagKey = "TUnit.Playwright.RecordingScope";
    private readonly Lock _lock = new();
    private readonly List<(Func<Task>? Settle, Func<ValueTask> Dispose, int Order)> _cleanup = [];
    private readonly List<PlaywrightFailureCapture> _captures = [];

    internal static PlaywrightRecordingScope For(TestContext context) =>
        (PlaywrightRecordingScope)context.StateBag.Items.GetOrAdd(StateBagKey, static _ => new PlaywrightRecordingScope())!;

    internal void Register(Func<ValueTask> cleanup, int order) => Register(null, cleanup, order);

    /// <param name="settle">Waits for pending setup; runs before captures are collected.</param>
    /// <param name="cleanup">Disposes the resource; runs after every capture.</param>
    /// <param name="order">Lower values are disposed first.</param>
    internal void Register(Func<Task>? settle, Func<ValueTask> cleanup, int order)
    {
        lock (_lock)
        {
            _cleanup.Add((settle, cleanup, order));
        }
    }

    internal void RegisterCapture(PlaywrightFailureCapture capture)
    {
        lock (_lock)
        {
            _captures.Add(capture);
        }
    }

    internal static ValueTask CompleteAsync(TestContext context) =>
        context.StateBag.TryGetValue<PlaywrightRecordingScope>(StateBagKey, out var scope) && scope is not null
            ? scope.CompleteAsync()
            : default;

    private async ValueTask CompleteAsync()
    {
        (Func<Task>? Settle, Func<ValueTask> Dispose, int Order)[] cleanup;
        lock (_lock)
        {
            _cleanup.Sort(static (left, right) => left.Order.CompareTo(right.Order));
            cleanup = _cleanup.ToArray();
            _cleanup.Clear();
        }

        // Setup that outlived a cancelled wait may still be creating a context and its capture.
        // Wait for it, so that capture is collected below and runs before its pages close.
        foreach (var (settle, _, _) in cleanup)
        {
            if (settle is not null)
            {
                await settle().ConfigureAwait(false);
            }
        }

        PlaywrightFailureCapture[] captures;
        lock (_lock)
        {
            captures = _captures.ToArray();
            _captures.Clear();
        }

        // Capture while pages are still open: disposal below closes them. Captures never throw.
        foreach (var capture in captures)
        {
            await capture.CompleteAsync().ConfigureAwait(false);
        }

        List<Exception>? exceptions = null;
        // Dispose pages before contexts, including when a page initialized its own context.
        foreach (var (_, dispose, _) in cleanup)
        {
            try
            {
                await dispose().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (exceptions ??= []).Add(exception);
            }
        }

        if (exceptions is not null)
        {
            throw new AggregateException("One or more Playwright fixtures failed to close.", exceptions);
        }
    }
}
