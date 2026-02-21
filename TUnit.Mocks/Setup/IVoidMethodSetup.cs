namespace TUnit.Mocks.Setup;

/// <summary>
/// Fluent setup builder for void methods.
/// </summary>
public interface IVoidMethodSetup
{
    /// <summary>Configure an exception to throw.</summary>
    IVoidSetupChain Throws<TException>() where TException : Exception, new();

    /// <summary>Configure a specific exception instance to throw.</summary>
    IVoidSetupChain Throws(Exception exception);

    /// <summary>Execute a callback when the method is called.</summary>
    IVoidSetupChain Callback(Action callback);

    /// <summary>Execute a callback with the method arguments when the method is called.</summary>
    IVoidSetupChain Callback(Action<object?[]> callback);

    /// <summary>Configure a computed exception based on method arguments to throw.</summary>
    IVoidSetupChain Throws(Func<object?[], Exception> exceptionFactory);

    /// <summary>Auto-raise the named event when this method is called.</summary>
    IVoidSetupChain Raises(string eventName, object? args = null);

    /// <summary>Assign a value to an out or ref parameter when this setup matches.</summary>
    IVoidSetupChain SetsOutParameter(int paramIndex, object? value);

    /// <summary>Transition to the named state after this setup's behavior executes.</summary>
    IVoidSetupChain TransitionsTo(string stateName);
}
