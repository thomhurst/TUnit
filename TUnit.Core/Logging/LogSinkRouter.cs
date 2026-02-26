namespace TUnit.Core.Logging;

/// <summary>
/// Internal helper for routing log messages to all registered sinks.
/// </summary>
internal static class LogSinkRouter
{
    // Use a TextWriter that bypasses any console interceptor to avoid recursion.
    // Console.OpenStandardError() returns the raw stderr stream, unaffected by Console.SetError().
    private static readonly TextWriter SafeStdErr = TextWriter.Synchronized(
        new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });

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
                // Write directly to raw stderr to avoid recursion through console interceptors
                SafeStdErr.WriteLine(
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
                // Write directly to raw stderr to avoid recursion through console interceptors
                await SafeStdErr.WriteLineAsync(
                    $"[TUnit] Log sink {sink.GetType().Name} failed: {ex.Message}").ConfigureAwait(false);
            }
        }
    }
}
