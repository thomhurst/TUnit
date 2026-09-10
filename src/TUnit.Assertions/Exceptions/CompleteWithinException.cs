namespace TUnit.Assertions.Exceptions;

internal class CompleteWithinException : AssertionException
{
    public CompleteWithinException(string? message) : base(message)
    {
    }

    public CompleteWithinException(string? message, Exception innerException) : base(message, innerException)
    {
    }
}
