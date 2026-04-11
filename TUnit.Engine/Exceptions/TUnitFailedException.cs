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
        // Pattern check (rather than `string.IsNullOrEmpty`) so the compiler narrows
        // `stackTrace` to non-null in the rest of the method on netstandard2.0,
        // where `IsNullOrEmpty` lacks `[NotNullWhen(false)]`.
        if (stackTrace is null || stackTrace.Length == 0)
        {
            return string.Empty;
        }

        var span = stackTrace.AsSpan();

        // Fast path: user-code failures usually throw from pure user frames with no
        // TUnit internals at all — skip the split/copy loop entirely in that case.
        if (span.IndexOf("at TUnit.".AsSpan()) < 0)
        {
            return stackTrace;
        }

        var vsb = new ValueStringBuilder(stackalloc char[1024]);
        var added = false;
        var omittedAny = false;
        var lastWasStripped = false;
        // ReadOnlySpan rather than Range to avoid a nullability warning from the
        // netstandard2.0 polyfill — Range is a value type but the polyfill annotates
        // `default` as a possible null assignment.
        ReadOnlySpan<char> pendingSeparator = default;
        var hasPendingSeparator = false;

        foreach (var range in span.Split(Environment.NewLine))
        {
            var slice = span[range];
            var trimmed = slice.Trim();

            if (IsTUnitInternalFrame(trimmed))
            {
                omittedAny = true;
                // A separator we were holding described resumption *into* the frame
                // we're now stripping — its anchor is gone, so drop it.
                hasPendingSeparator = false;
                lastWasStripped = true;
                continue;
            }

            var isFrame = trimmed.StartsWith("at ");
            if (isFrame)
            {
                if (added)
                {
                    vsb.Append(Environment.NewLine);
                }

                if (hasPendingSeparator)
                {
                    vsb.Append(pendingSeparator);
                    vsb.Append(Environment.NewLine);
                    hasPendingSeparator = false;
                }

                vsb.Append(slice);
                added = true;
                lastWasStripped = false;
                continue;
            }

            // Separator/non-frame line: only emit if it sits between two surviving
            // user frames. Drop it if nothing has been emitted yet (would orphan at
            // the top), or if the previous non-separator line was a stripped TUnit
            // frame (its "before" anchor is gone). Otherwise buffer — we'll flush
            // it when (and only when) a surviving user frame follows.
            if (!added || lastWasStripped)
            {
                continue;
            }

            pendingSeparator = slice;
            hasPendingSeparator = true;
        }

        // If every frame is TUnit-internal, the error originates inside TUnit itself —
        // return the full unfiltered trace so the bug can be diagnosed. Also return
        // the original if nothing was actually omitted (the fast-path `at TUnit.` match
        // landed inside a non-frame line, e.g. an embedded message).
        if (!added || !omittedAny)
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
