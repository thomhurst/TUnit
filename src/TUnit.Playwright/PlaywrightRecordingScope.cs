using TUnit.Core;

namespace TUnit.Playwright;

internal sealed class PlaywrightRecordingScope
{
    private const string StateBagKey = "TUnit.Playwright.RecordingScope";
    private readonly Lock _lock = new();
    private readonly List<(Func<ValueTask> Dispose, int Order)> _cleanup = [];

    internal static PlaywrightRecordingScope For(TestContext context) =>
        (PlaywrightRecordingScope)context.StateBag.Items.GetOrAdd(StateBagKey, static _ => new PlaywrightRecordingScope())!;

    internal void Register(Func<ValueTask> cleanup, int order)
    {
        lock (_lock)
        {
            _cleanup.Add((cleanup, order));
        }
    }

    internal static ValueTask CompleteAsync(TestContext context) =>
        context.StateBag.TryGetValue<PlaywrightRecordingScope>(StateBagKey, out var scope) && scope is not null
            ? scope.CompleteAsync()
            : default;

    private async ValueTask CompleteAsync()
    {
        (Func<ValueTask> Dispose, int Order)[] cleanup;
        lock (_lock)
        {
            _cleanup.Sort(static (left, right) => left.Order.CompareTo(right.Order));
            cleanup = _cleanup.ToArray();
            _cleanup.Clear();
        }

        List<Exception>? exceptions = null;
        // Dispose pages before contexts, including when a page initialized its own context.
        foreach (var (dispose, _) in cleanup)
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
