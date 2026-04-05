using System.ComponentModel;
using TUnit.Mocks.Setup.Behaviors;

namespace TUnit.Mocks.Setup;

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
        _setup.AddBehavior(new ComputedThrowBehavior(_ => new TException()));
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback(Action<object?[]> callback)
    {
        _setup.AddBehavior(new CallbackWithArgsBehavior(callback));
        return this;
    }

    /// <summary>Typed callback overload emitted by the source generator. Avoids boxing arguments into object?[].</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback<T1>(Action<T1> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback<T1, T2>(Action<T1, T2> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback<T1, T2, T3>(Action<T1, T2, T3> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback<T1, T2, T3, T4>(Action<T1, T2, T3, T4> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4, T5>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4, T5, T6>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4, T5, T6, T7>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4, T5, T6, T7, T8>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Returns(Func<object?[], TReturn> factory)
    {
        _setup.AddBehavior(new ComputedReturnWithArgsBehavior<TReturn>(factory));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Throws(Func<object?[], Exception> exceptionFactory)
    {
        _setup.AddBehavior(new ComputedThrowBehavior(exceptionFactory));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Raises(string eventName, object? args = null)
    {
        _setup.AddEventRaise(new EventRaiseInfo(eventName, args));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> SetsOutParameter(int paramIndex, object? value)
    {
        _setup.SetOutRefValue(paramIndex, value);
        return this;
    }

    public ISetupChain<TReturn> TransitionsTo(string stateName)
    {
        _setup.TransitionTarget = stateName;
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> ReturnsRaw(object? rawValue)
    {
        _setup.AddBehavior(new RawReturnBehavior(rawValue));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> ReturnsRaw(Func<object?> factory)
    {
        _setup.AddBehavior(new ComputedRawReturnBehavior(factory));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> ReturnsRaw(Func<object?[], object?> factory)
    {
        _setup.AddBehavior(new ComputedRawReturnWithArgsBehavior(factory));
        return this;
    }

    public IMethodSetup<TReturn> Then() => this;
}
