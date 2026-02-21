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

    /// <summary>Assign a value to an out or ref parameter when this setup matches.</summary>
    /// <param name="paramIndex">The zero-based index of the parameter in the method signature.</param>
    /// <param name="value">The value to assign.</param>
    ISetupChain<TReturn> SetsOutParameter(int paramIndex, object? value);
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

    /// <summary>Assign a value to an out or ref parameter when this setup matches.</summary>
    /// <param name="paramIndex">The zero-based index of the parameter in the method signature.</param>
    /// <param name="value">The value to assign.</param>
    IVoidSetupChain SetsOutParameter(int paramIndex, object? value);
}
