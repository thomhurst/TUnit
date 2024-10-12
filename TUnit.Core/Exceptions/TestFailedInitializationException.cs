namespace TUnit.Core.Exceptions;

public class TestFailedInitializationException(string? message, Exception? innerException)
    : Exception(message, innerException);