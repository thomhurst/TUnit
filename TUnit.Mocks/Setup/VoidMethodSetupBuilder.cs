using System.ComponentModel;
using TUnit.Mocks.Setup.Behaviors;

namespace TUnit.Mocks.Setup;

/// <summary>
/// Concrete implementation of setup builder for void methods.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class VoidMethodSetupBuilder : IVoidMethodSetup, IVoidSetupChain
{
    private readonly MethodSetup _setup;

    public VoidMethodSetupBuilder(MethodSetup setup)
    {
        _setup = setup;
    }

    public IVoidSetupChain Throws<TException>() where TException : Exception, new()
    {
        _setup.AddBehavior(new ComputedThrowBehavior(_ => new TException()));
        return this;
    }

    public IVoidSetupChain Throws(Exception exception)
    {
        _setup.AddBehavior(new ThrowBehavior(exception));
        return this;
    }

    public IVoidSetupChain Callback(Action callback)
    {
        _setup.AddBehavior(new CallbackBehavior(callback));
        return this;
    }

    public IVoidSetupChain Callback(Action<object?[]> callback)
    {
        _setup.AddBehavior(new CallbackWithArgsBehavior(callback));
        return this;
    }

    public IVoidSetupChain Throws(Func<object?[], Exception> exceptionFactory)
    {
        _setup.AddBehavior(new ComputedThrowBehavior(exceptionFactory));
        return this;
    }

    public IVoidSetupChain Raises(string eventName, object? args = null)
    {
        _setup.AddEventRaise(new EventRaiseInfo(eventName, args));
        return this;
    }

    public IVoidSetupChain SetsOutParameter(int paramIndex, object? value)
    {
        _setup.SetOutRefValue(paramIndex, value);
        return this;
    }

    public IVoidSetupChain TransitionsTo(string stateName)
    {
        _setup.TransitionTarget = stateName;
        return this;
    }

    public IVoidMethodSetup Then() => this;
}
