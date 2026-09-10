namespace TUnit.Assertions.Exceptions;

/// <summary>
/// Wraps exceptions that are caught by AssertionScope's FirstChanceException handler.
/// These exceptions may or may not have been caught by user code,
/// so we need to distinguish them from actual assertion failures.
/// </summary>
public class MaybeCaughtException(Exception exception)
    : Exception($"(This exception may or may not have been caught) {exception.GetType().Namespace}.{exception.GetType().Name}: {exception.Message}", exception);
