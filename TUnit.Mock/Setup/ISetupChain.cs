namespace TUnit.Mock.Setup;

/// <summary>
/// Setup chain for sequential behavior configuration on methods with a return value.
/// </summary>
public interface ISetupChain<TReturn>
{
    /// <summary>Chain the next call's behavior.</summary>
    IMethodSetup<TReturn> Then();

    /// <summary>Auto-raise the named event when this method is called.</summary>
    ISetupChain<TReturn> Raises(string eventName, object? args = null);
}

/// <summary>
/// Setup chain for void method sequential behavior configuration.
/// </summary>
public interface IVoidSetupChain
{
    /// <summary>Chain the next call's behavior.</summary>
    IVoidMethodSetup Then();

    /// <summary>Auto-raise the named event when this method is called.</summary>
    IVoidSetupChain Raises(string eventName, object? args = null);
}
