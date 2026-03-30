namespace TUnit.Mocks.Setup;

/// <summary>
/// Setup chain for sequential behavior configuration on methods with a return value.
/// </summary>
public interface ISetupChain<TReturn>
{
    /// <summary>Chain the next call's behavior.</summary>
    IMethodSetup<TReturn> Then();

    /// <summary>Transition to the named state after this behavior executes.</summary>
    ISetupChain<TReturn> TransitionsTo(string stateName);
}

/// <summary>
/// Setup chain for void method sequential behavior configuration.
/// </summary>
public interface IVoidSetupChain
{
    /// <summary>Chain the next call's behavior.</summary>
    IVoidMethodSetup Then();

    /// <summary>Transition to the named state after this behavior executes.</summary>
    IVoidSetupChain TransitionsTo(string stateName);
}
