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

    private static string FilterStackTrace(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return string.Empty;
        }

        var vsb = new ValueStringBuilder(stackalloc char[256]);

        var added = false;
        foreach(var range in stackTrace.AsSpan().Split(Environment.NewLine))
        {
            var slice = stackTrace.AsSpan()[range];
            if (slice.Trim().StartsWith("at TUnit"))
            {
                break;
            }
            vsb.Append(added ? Environment.NewLine : "");
            vsb.Append(slice);
            added = true;
        }

        return vsb.ToString();
    }
}
