using System.ComponentModel;
using TUnit.Mock.Setup.Behaviors;

namespace TUnit.Mock.Setup;

/// <summary>
/// Concrete implementation of setup builder for methods returning TReturn.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MethodSetupBuilder<TReturn> : IMethodSetup<TReturn>, ISetupChain<TReturn>
{
    private readonly MethodSetup _setup;

    public MethodSetupBuilder(MethodSetup setup)
    {
        _setup = setup;
    }

    public ISetupChain<TReturn> Returns(TReturn value)
    {
        _setup.AddBehavior(new ReturnBehavior<TReturn>(value));
        return this;
    }

    public ISetupChain<TReturn> Returns(Func<TReturn> factory)
    {
        _setup.AddBehavior(new ComputedReturnBehavior<TReturn>(factory));
        return this;
    }

    public ISetupChain<TReturn> ReturnsSequentially(params TReturn[] values)
    {
        foreach (var value in values)
        {
            _setup.AddBehavior(new ReturnBehavior<TReturn>(value));
        }

        return this;
    }

    public ISetupChain<TReturn> Throws<TException>() where TException : Exception, new()
    {
        _setup.AddBehavior(new ThrowBehavior(new TException()));
        return this;
    }

    public ISetupChain<TReturn> Throws(Exception exception)
    {
        _setup.AddBehavior(new ThrowBehavior(exception));
        return this;
    }

    public ISetupChain<TReturn> Callback(Action callback)
    {
        _setup.AddBehavior(new CallbackBehavior(callback));
        return this;
    }

    public ISetupChain<TReturn> Callback(Action<object?[]> callback)
    {
        _setup.AddBehavior(new CallbackWithArgsBehavior(callback));
        return this;
    }

    public ISetupChain<TReturn> Returns(Func<object?[], TReturn> factory)
    {
        _setup.AddBehavior(new ComputedReturnWithArgsBehavior<TReturn>(factory));
        return this;
    }

    public ISetupChain<TReturn> Throws(Func<object?[], Exception> exceptionFactory)
    {
        _setup.AddBehavior(new ComputedThrowBehavior(exceptionFactory));
        return this;
    }

    public ISetupChain<TReturn> Raises(string eventName, object? args = null)
    {
        _setup.AddEventRaise(new EventRaiseInfo(eventName, args));
        return this;
    }

    public ISetupChain<TReturn> SetsOutParameter(int paramIndex, object? value)
    {
        _setup.SetOutRefValue(paramIndex, value);
        return this;
    }

    public IMethodSetup<TReturn> Then() => this;
}
