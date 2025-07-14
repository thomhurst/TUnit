using TUnit.Core.Exceptions;

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

        var lines = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        return string.Join(Environment.NewLine,
            lines.TakeWhile(x => !x.Trim().StartsWith("at TUnit")));
    }
}
