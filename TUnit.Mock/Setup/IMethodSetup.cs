namespace TUnit.Mock.Setup;

/// <summary>
/// Fluent setup builder for methods with a return value.
/// Returned by mock.Setup.MethodName(args).
/// </summary>
public interface IMethodSetup<TReturn>
{
    /// <summary>Configure a fixed return value. For async methods, the source generator wraps automatically.</summary>
    ISetupChain<TReturn> Returns(TReturn value);

    /// <summary>Configure a computed return value.</summary>
    ISetupChain<TReturn> Returns(Func<TReturn> factory);

    /// <summary>Configure sequential return values.</summary>
    ISetupChain<TReturn> ReturnsSequentially(params TReturn[] values);

    /// <summary>Configure an exception to throw.</summary>
    ISetupChain<TReturn> Throws<TException>() where TException : Exception, new();

    /// <summary>Configure a specific exception instance to throw.</summary>
    ISetupChain<TReturn> Throws(Exception exception);

    /// <summary>Execute a callback when the method is called.</summary>
    ISetupChain<TReturn> Callback(Action callback);

    /// <summary>Auto-raise the named event when this method is called.</summary>
    ISetupChain<TReturn> Raises(string eventName, object? args = null);
}
