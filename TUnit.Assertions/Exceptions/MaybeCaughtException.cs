namespace TUnit.Assertions.Exceptions;

public class MaybeCaughtException(Exception exception)
    : Exception($"(This exception may or may not have been caught) {exception.GetType().Namespace}.{exception.GetType().Name}: {exception.Message}", exception);