using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

/// <summary>
/// Records when a browser that TUnit launched disconnects without TUnit closing it, so tests that
/// then fail with <c>TargetClosedException</c> report the one root cause instead of N unrelated errors.
/// </summary>
internal sealed class PlaywrightBrowserMonitor
{
    private const string UsedBrowsersKey = "TUnit.Playwright.UsedBrowsers";
    private const string ReportedKey = "TUnit.Playwright.BrowserDisconnectReported";
    private static readonly ConditionalWeakTable<IBrowser, PlaywrightBrowserMonitor> Monitors = new();

    private readonly string _browserName;
    private long _disconnectedAtTicks;
    private int _closing;

    private PlaywrightBrowserMonitor(IBrowser browser)
    {
        _browserName = SafeBrowserName(browser);
        browser.Disconnected += OnDisconnected;
    }

    /// <summary>When the browser disconnected unexpectedly, in UTC; otherwise <c>null</c>.</summary>
    internal DateTimeOffset? DisconnectedAt
    {
        get
        {
            var ticks = Interlocked.Read(ref _disconnectedAtTicks);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    /// <summary>Starts monitoring <paramref name="browser"/>, or returns the existing monitor.</summary>
    internal static PlaywrightBrowserMonitor For(IBrowser browser) =>
        Monitors.GetValue(browser, static key => new PlaywrightBrowserMonitor(key));

    /// <summary>Marks the browser as closed by TUnit, so its disconnection is expected.</summary>
    internal static void MarkClosing(IBrowser? browser)
    {
        if (browser is not null)
        {
            Interlocked.Exchange(ref For(browser)._closing, 1);
        }
    }

    /// <summary>Records that <paramref name="owner"/> depends on <paramref name="browser"/>.</summary>
    internal static PlaywrightBrowserMonitor RecordUse(TestContext? owner, IBrowser browser) =>
        RecordUse(owner, For(browser));

    /// <summary>
    /// Records that <paramref name="owner"/> depends on the monitored browser. Tests that reuse a
    /// shared fixture call this too, because only the first test created its context.
    /// </summary>
    internal static PlaywrightBrowserMonitor RecordUse(TestContext? owner, PlaywrightBrowserMonitor monitor)
    {
        if (owner is not null)
        {
            UsedBrowsers.For(owner).Add(monitor);
        }

        return monitor;
    }

    internal string Describe() =>
        DisconnectedAt is { } at
            ? $"The shared {_browserName} browser disconnected at {at:yyyy-MM-dd HH:mm:ss.fff} UTC, before this failure. " +
              "The browser probably crashed or was closed; failures in tests that share it have this same root cause."
            : string.Empty;

    /// <summary>
    /// Wraps a context creation failure caused by a disconnected browser, so the fixture error
    /// names the root cause. Returns <c>null</c> when the browser is still connected.
    /// </summary>
    internal Exception? WrapIfDisconnected(Exception exception, TestContext? owner)
    {
        if (DisconnectedAt is null || exception is not PlaywrightException)
        {
            return null;
        }

        if (owner is not null)
        {
            owner.StateBag.Items.TryAdd(ReportedKey, true);
        }

        return new InvalidOperationException(Describe(), exception);
    }

    /// <summary>
    /// Adds a note to a failed test whose Playwright failure follows the disconnection of a
    /// browser it used. Writes the note once per test.
    /// </summary>
    internal static void ReportIfDisconnected(TestContext context)
    {
        if (context.Execution.Result is not { State: not (TestState.Passed or TestState.Skipped) } result ||
            !context.StateBag.TryGetValue<UsedBrowsers>(UsedBrowsersKey, out var used) ||
            used is null ||
            !IsPlaywrightFailure(result.Exception))
        {
            return;
        }

        foreach (var monitor in used.Snapshot())
        {
            if (monitor.DisconnectedAt is null)
            {
                continue;
            }

            if (context.StateBag.Items.TryAdd(ReportedKey, true))
            {
                context.Output.ErrorOutput.WriteLine(monitor.Describe());
            }

            return;
        }
    }

    private static bool IsPlaywrightFailure(Exception? exception)
    {
        for (var depth = 0; exception is not null && depth < 16; depth++)
        {
            if (exception is PlaywrightException)
            {
                return true;
            }

            if (exception is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    if (IsPlaywrightFailure(inner))
                    {
                        return true;
                    }
                }

                return false;
            }

            exception = exception.InnerException;
        }

        return false;
    }

    private void OnDisconnected(object? sender, IBrowser browser)
    {
        if (Volatile.Read(ref _closing) == 0)
        {
            Interlocked.CompareExchange(ref _disconnectedAtTicks, DateTimeOffset.UtcNow.UtcTicks, 0);
        }
    }

    private static string SafeBrowserName(IBrowser browser)
    {
        try
        {
            return browser.BrowserType?.Name ?? "Playwright";
        }
        catch
        {
            return "Playwright";
        }
    }

    /// <summary>The browsers one test depends on.</summary>
    private sealed class UsedBrowsers
    {
        private readonly Lock _lock = new();
        private readonly List<PlaywrightBrowserMonitor> _monitors = [];

        internal static UsedBrowsers For(TestContext context) =>
            (UsedBrowsers)context.StateBag.Items.GetOrAdd(UsedBrowsersKey, static _ => new UsedBrowsers())!;

        internal void Add(PlaywrightBrowserMonitor monitor)
        {
            lock (_lock)
            {
                if (!_monitors.Contains(monitor))
                {
                    _monitors.Add(monitor);
                }
            }
        }

        internal PlaywrightBrowserMonitor[] Snapshot()
        {
            lock (_lock)
            {
                return _monitors.ToArray();
            }
        }
    }
}
