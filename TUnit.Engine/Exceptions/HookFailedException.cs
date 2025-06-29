namespace TUnit.Engine.Exceptions;

public class HookFailedException : TUnitFailedException
{
    public HookFailedException(Exception exception) : base(exception)
    {
    }

    public HookFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
