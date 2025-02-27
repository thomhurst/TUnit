using TUnit.Core.Exceptions;

namespace TUnit.Engine.Exceptions;

public class TestFailedException(Exception exception) : TUnitException($"{exception.GetType().Name}: {exception.Message}", exception.InnerException)
{
    public override string StackTrace { get; } = FilterStackTrace(exception.StackTrace);
    
    private static string FilterStackTrace(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return string.Empty;
        }
        
        var lines = stackTrace!.Split([Environment.NewLine], StringSplitOptions.None);

        return string.Join(Environment.NewLine,
            lines.TakeWhile(x => !x.Trim().StartsWith("at TUnit")));
    }
}