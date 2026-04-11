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

    // The hint only mentions --detailed-stacktrace because filtering is bypassed
    // entirely when --log-level Debug/Trace is set (see TUnitMessageBus.SimplifyStacktrace),
    // so users on debug logging will never see this message.
    private const string OmittedHint =
        $"   --- TUnit internals omitted (run with --{DetailedStacktraceCommandProvider.DetailedStackTrace} for full trace) ---";

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

            var isFrame = trimmed.StartsWith("at ");

            // Only actual stack frames count as user frames, not separator lines
            // like "--- End of stack trace from previous location ---"
            if (isFrame)
            {
                hasUserFrame = true;
            }

            // Skip separator/non-frame lines until we've emitted a frame, otherwise
            // an "End of stack trace from previous location" line that originally
            // followed a stripped TUnit frame would orphan at the top of the output.
            if (!added && !isFrame)
            {
                continue;
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
        vsb.Append(OmittedHint);

        return vsb.ToString();
    }

    private static bool IsTUnitInternalFrame(ReadOnlySpan<char> trimmedLine)
    {
        // Uses "at TUnit." with the dot to avoid false positives on user types whose names
        // happen to start with "TUnit" (e.g. a hypothetical "TUnitExtensions" namespace).
        return trimmedLine.StartsWith("at TUnit.");
    }
}
