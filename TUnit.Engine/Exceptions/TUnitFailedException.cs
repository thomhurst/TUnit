using TUnit.Core.Exceptions;
using TUnit.Core.Helpers;

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

        // First pass: check if any non-TUnit frames exist.
        // If every frame is TUnit-internal, the error originates inside TUnit itself —
        // keep the full trace so the bug can be diagnosed.
        var hasUserFrame = false;
        foreach (var range in stackTrace.AsSpan().Split(Environment.NewLine))
        {
            var trimmed = stackTrace.AsSpan()[range].Trim();
            if (trimmed.Length > 0 && !IsTUnitInternalFrame(trimmed))
            {
                hasUserFrame = true;
                break;
            }
        }

        if (!hasUserFrame)
        {
            return stackTrace;
        }

        // Second pass: remove TUnit-internal frames but keep all user frames,
        // even if they appear after TUnit frames (e.g. assertion internals above
        // the user's test method).
        var vsb = new ValueStringBuilder(stackalloc char[256]);
        var added = false;
        var omittedAny = false;

        foreach (var range in stackTrace.AsSpan().Split(Environment.NewLine))
        {
            var slice = stackTrace.AsSpan()[range];
            var trimmed = slice.Trim();

            if (IsTUnitInternalFrame(trimmed))
            {
                omittedAny = true;
                continue;
            }

            vsb.Append(added ? Environment.NewLine : "");
            vsb.Append(slice);
            added = true;
        }

        if (omittedAny)
        {
            vsb.Append(Environment.NewLine);
            vsb.Append("   --- TUnit internals omitted (run with --detailed-stacktrace for full trace) ---");
        }

        return vsb.ToString();
    }

    private static bool IsTUnitInternalFrame(ReadOnlySpan<char> trimmedLine)
    {
        // Match frames from TUnit's own namespaces (TUnit.Engine, TUnit.Core, TUnit.Assertions, etc.)
        // Uses "at TUnit." with the dot to avoid false positives on user types whose names
        // happen to start with "TUnit" (e.g. a hypothetical "TUnitExtensions" namespace).
        return trimmedLine.StartsWith("at TUnit.");
    }
}
