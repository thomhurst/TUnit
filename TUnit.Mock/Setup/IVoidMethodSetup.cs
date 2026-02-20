namespace TUnit.Mock.Setup;

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
}
