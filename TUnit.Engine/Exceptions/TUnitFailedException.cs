using TUnit.Core.Exceptions;
using TUnit.Core.Helpers;
using TUnit.Engine.CommandLineProviders;

namespace TUnit.Engine.Exceptions;

public abstract class TUnitFailedException : TUnitException
{
    protected TUnitFailedException(Exception exception) : base($"{exception.GetType().Name}: {exception.Message}", exception.InnerException)
    {
        StackTrace = FilterStackTrace(exception.StackTrace);
    }

    protected TUnitFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
        StackTrace = FilterStackTrace(innerException?.StackTrace);
    }

    public override string StackTrace { get; }

    internal static string FilterStackTrace(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return string.Empty;
        }

        var span = stackTrace.AsSpan();
        var vsb = new ValueStringBuilder(stackalloc char[1024]);
        var added = false;
        var omittedAny = false;
        var hasUserFrame = false;

        foreach (var range in span.Split(Environment.NewLine))
        {
            var slice = span[range];
            var trimmed = slice.Trim();

            if (IsTUnitInternalFrame(trimmed))
            {
                omittedAny = true;
                continue;
            }

            // Only actual stack frames count as user frames, not separator lines
            // like "--- End of stack trace from previous location ---"
            if (trimmed.StartsWith("at "))
            {
                hasUserFrame = true;
            }

            if (added)
            {
                vsb.Append(Environment.NewLine);
            }

            vsb.Append(slice);
            added = true;
        }

        // If every frame is TUnit-internal, the error originates inside TUnit itself —
        // return the full unfiltered trace so the bug can be diagnosed.
        // Also return the original when nothing was omitted (no TUnit frames at all).
        if (!hasUserFrame || !omittedAny)
        {
            vsb.Dispose();
            return stackTrace;
        }

        vsb.Append(Environment.NewLine);
        vsb.Append($"   --- TUnit internals omitted (run with --{DetailedStacktraceCommandProvider.DetailedStackTrace} for full trace) ---");

        return vsb.ToString();
    }

    private static bool IsTUnitInternalFrame(ReadOnlySpan<char> trimmedLine)
    {
        // Uses "at TUnit." with the dot to avoid false positives on user types whose names
        // happen to start with "TUnit" (e.g. a hypothetical "TUnitExtensions" namespace).
        return trimmedLine.StartsWith("at TUnit.");
    }
}
