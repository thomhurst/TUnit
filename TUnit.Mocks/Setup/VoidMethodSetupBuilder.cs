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

    public IVoidSetupChain Returns()
    {
        _setup.AddBehavior(VoidReturnBehavior.Instance);
        return this;
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback(Action<object?[]> callback)
    {
        _setup.AddBehavior(new CallbackWithArgsBehavior(callback));
        return this;
    }

    // ── ARITY COUPLING (1–8): keep in sync with MethodSetupBuilder,
    //    TypedCallbackBehavior.cs, MockEngine.Typed.cs, and MaxTypedParams in MockMembersBuilder.cs

    /// <summary>Typed callback overload emitted by the source generator. Avoids boxing arguments into object?[].</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback<T1>(Action<T1> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback<T1, T2>(Action<T1, T2> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback<T1, T2, T3>(Action<T1, T2, T3> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback<T1, T2, T3, T4>(Action<T1, T2, T3, T4> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4, T5>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4, T5, T6>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4, T5, T6, T7>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> callback)
    {
        _setup.AddBehavior(new TypedCallbackBehavior<T1, T2, T3, T4, T5, T6, T7, T8>(callback));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Throws(Func<object?[], Exception> exceptionFactory)
    {
        _setup.AddBehavior(new ComputedThrowBehavior(exceptionFactory));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Raises(string eventName, object? args = null)
    {
        _setup.AddEventRaise(new EventRaiseInfo(eventName, args));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain ReturnsRaw(object? rawValue)
    {
        _setup.AddBehavior(new RawReturnBehavior(rawValue));
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain ReturnsRaw(Func<object?> factory)
    {
        _setup.AddBehavior(new ComputedRawReturnBehavior(factory));
        return this;
    }

    public IVoidMethodSetup Then() => this;
}
