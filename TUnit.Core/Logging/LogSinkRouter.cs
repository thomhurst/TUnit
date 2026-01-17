namespace TUnit.Core.Logging;

/// <summary>
/// Internal helper for routing log messages to all registered sinks.
/// </summary>
internal static class LogSinkRouter
{
    public static void RouteToSinks(LogLevel level, string message, Exception? exception, Context? context)
    {
        var sinks = TUnitLoggerFactory.GetSinks();
        if (sinks.Count == 0)
        {
            return;
        }

        foreach (var sink in sinks)
        {
            if (!sink.IsEnabled(level))
            {
                continue;
            }

            try
            {
                sink.Log(level, message, exception, context);
            }
            catch (Exception ex)
            {
                // Write to original console to avoid recursion
                GlobalContext.Current.OriginalConsoleError.WriteLine(
                    $"[TUnit] Log sink {sink.GetType().Name} failed: {ex.Message}");
            }
        }
    }

    public static async ValueTask RouteToSinksAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        var sinks = TUnitLoggerFactory.GetSinks();
        if (sinks.Count == 0)
        {
            return;
        }

        foreach (var sink in sinks)
        {
            if (!sink.IsEnabled(level))
            {
                continue;
            }

            try
            {
                await sink.LogAsync(level, message, exception, context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Write to original console to avoid recursion
                await GlobalContext.Current.OriginalConsoleError.WriteLineAsync(
                    $"[TUnit] Log sink {sink.GetType().Name} failed: {ex.Message}").ConfigureAwait(false);
            }
        }
    }
}
