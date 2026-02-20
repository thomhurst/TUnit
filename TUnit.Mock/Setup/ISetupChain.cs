namespace TUnit.Mock.Setup;

/// <summary>
/// Setup chain for sequential behavior configuration on methods with a return value.
/// </summary>
public interface ISetupChain<TReturn>
{
    /// <summary>Chain the next call's behavior.</summary>
    IMethodSetup<TReturn> Then();
}

/// <summary>
/// Setup chain for void method sequential behavior configuration.
/// </summary>
public interface IVoidSetupChain
{
    /// <summary>Chain the next call's behavior.</summary>
    IVoidMethodSetup Then();
}
