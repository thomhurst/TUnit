using System.Collections.Concurrent;

namespace TUnit.Logging.Microsoft;

/// <summary>
/// Internal registry tracking which test contexts have per-test logging active.
/// Used by the correlated logger to avoid duplicate output when both per-test
/// and correlated loggers are registered (e.g., isolated factories inheriting
/// shared factory configuration).
/// </summary>
internal static class TUnitLoggingRegistry
{
    internal static readonly ConcurrentDictionary<string, bool> PerTestLoggingActive = new();
}
