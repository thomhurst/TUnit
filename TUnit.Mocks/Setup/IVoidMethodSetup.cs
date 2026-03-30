namespace TUnit.Mocks.Setup;

/// <summary>
/// Fluent setup builder for void methods.
/// </summary>
public interface IVoidMethodSetup
{
    /// <summary>Configure the method to return normally (no-op). Useful for sequential behavior chains.</summary>
    IVoidSetupChain Returns();

    /// <summary>Configure an exception to throw.</summary>
    IVoidSetupChain Throws<TException>() where TException : Exception, new();

    /// <summary>Configure a specific exception instance to throw.</summary>
    IVoidSetupChain Throws(Exception exception);

    /// <summary>Execute a callback when the method is called.</summary>
    IVoidSetupChain Callback(Action callback);

    /// <summary>Transition to the named state after this setup's behavior executes.</summary>
    IVoidSetupChain TransitionsTo(string stateName);
}
