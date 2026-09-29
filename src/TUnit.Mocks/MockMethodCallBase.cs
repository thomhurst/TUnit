using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Shared plumbing for the generated per-method <c>*_MockCall</c> wrappers of non-void methods:
/// lazy setup registration, the parameter-independent setup methods and call verification.
/// The generated wrapper derives from this and adds only its per-method typed overloads.
/// Setup methods return <typeparamref name="TSelf"/> so chaining keeps the generated wrapper type.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class MockMethodCallBase<TSelf, TReturn> : ICallVerification
    where TSelf : MockMethodCallBase<TSelf, TReturn>
{
    private readonly IMockEngineAccess _engine;
    private readonly int _memberId;
    private readonly string _memberName;
    private readonly IArgumentMatcher[] _matchers;
    private readonly ImmutableArray<Type> _typeArguments;
    private MethodSetupBuilder<TReturn>? _builder;

    [EditorBrowsable(EditorBrowsableState.Never)]
    protected MockMethodCallBase(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers, ImmutableArray<Type> typeArguments)
    {
        _engine = engine;
        _memberId = memberId;
        _memberName = memberName;
        _matchers = matchers;
        _typeArguments = typeArguments;
    }

    /// <summary>Returns the setup builder, registering the setup with the engine on first use.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected MethodSetupBuilder<TReturn> EnsureSetup()
    {
        var existing = Volatile.Read(ref _builder);
        if (existing is not null) return existing;
        return EnsureSetupSlow();
    }

    // Race note: if two threads lose/win the CAS, the loser returns before the winner
    // calls AddSetup. Mock setup is sequential in normal usage (test setup phase completes
    // before invocations), so this window is not observable. If you ever need concurrent
    // setup + invocation of the same method-wrapper, reintroduce synchronization here.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private MethodSetupBuilder<TReturn> EnsureSetupSlow()
    {
        var setup = new MethodSetup(_memberId, _matchers, _memberName, _typeArguments);
        var fresh = new MethodSetupBuilder<TReturn>(setup);
        var prev = Interlocked.CompareExchange(ref _builder, fresh, null);
        if (prev is not null) return prev;
        _engine.AddSetup(setup);
        return fresh;
    }

    /// <summary>Configure a fixed return value.</summary>
    public TSelf Returns(TReturn value) { EnsureSetup().Returns(value); return (TSelf)this; }
    /// <summary>Configure a computed return value, invoked on each call.</summary>
    public TSelf Returns(Func<TReturn> factory) { EnsureSetup().Returns(factory); return (TSelf)this; }
    /// <summary>Configure sequential return values.</summary>
    public TSelf ReturnsSequentially(params TReturn[] values) { EnsureSetup().ReturnsSequentially(values); return (TSelf)this; }
    /// <summary>Configure the method to throw an exception of the specified type.</summary>
    public TSelf Throws<TException>() where TException : Exception, new() { EnsureSetup().Throws<TException>(); return (TSelf)this; }
    /// <summary>Configure the method to throw the specified exception.</summary>
    public TSelf Throws(Exception exception) { EnsureSetup().Throws(exception); return (TSelf)this; }
    /// <summary>Execute a callback when the method is called.</summary>
    public TSelf Callback(Action callback) { EnsureSetup().Callback(callback); return (TSelf)this; }
    /// <summary>Transition the mock to the specified state after this setup matches.</summary>
    public TSelf TransitionsTo(string stateName) { EnsureSetup().TransitionsTo(stateName); return (TSelf)this; }
    /// <summary>Chain the next sequential behavior.</summary>
    public TSelf Then() { EnsureSetup().Then(); return (TSelf)this; }

    private ICallVerification CreateVerification()
        => MockCallVerification.Create(_engine, _memberId, _memberName, _matchers, _typeArguments);

    /// <inheritdoc />
    public void WasCalled() => CreateVerification().WasCalled();
    /// <inheritdoc />
    public void WasCalled(Times times) => CreateVerification().WasCalled(times);
    /// <inheritdoc />
    public void WasCalled(Times times, string? message) => CreateVerification().WasCalled(times, message);
    /// <inheritdoc />
    public void WasCalled(string? message) => CreateVerification().WasCalled(message);
    /// <inheritdoc />
    public void WasNeverCalled() => CreateVerification().WasNeverCalled();
    /// <inheritdoc />
    public void WasNeverCalled(string? message) => CreateVerification().WasNeverCalled(message);
}

/// <summary>
/// Shared plumbing for the generated per-method <c>*_MockCall</c> wrappers of void (and
/// ref-struct-returning) methods. The setup is registered eagerly in the constructor so that
/// standalone calls like <c>mock.Log(Arg.Any&lt;string&gt;())</c> work in strict mode without chaining.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class VoidMockMethodCallBase<TSelf> : ICallVerification
    where TSelf : VoidMockMethodCallBase<TSelf>
{
    private readonly IMockEngineAccess _engine;
    private readonly int _memberId;
    private readonly string _memberName;
    private readonly IArgumentMatcher[] _matchers;
    private readonly ImmutableArray<Type> _typeArguments;
    private VoidMethodSetupBuilder? _builder;

    [EditorBrowsable(EditorBrowsableState.Never)]
    protected VoidMockMethodCallBase(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers, ImmutableArray<Type> typeArguments)
    {
        _engine = engine;
        _memberId = memberId;
        _memberName = memberName;
        _matchers = matchers;
        _typeArguments = typeArguments;
        _ = EnsureSetup();
    }

    /// <summary>Returns the setup builder, registering the setup with the engine on first use.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected VoidMethodSetupBuilder EnsureSetup()
    {
        var existing = Volatile.Read(ref _builder);
        if (existing is not null) return existing;
        return EnsureSetupSlow();
    }

    // See MockMethodCallBase.EnsureSetupSlow for the race note.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private VoidMethodSetupBuilder EnsureSetupSlow()
    {
        var setup = new MethodSetup(_memberId, _matchers, _memberName, _typeArguments);
        var fresh = new VoidMethodSetupBuilder(setup);
        var prev = Interlocked.CompareExchange(ref _builder, fresh, null);
        if (prev is not null) return prev;
        _engine.AddSetup(setup);
        return fresh;
    }

    /// <summary>Configure the method to complete normally (explicitly allows the call in strict mode).</summary>
    public TSelf Returns() { EnsureSetup().Returns(); return (TSelf)this; }
    /// <summary>Configure the method to throw an exception of the specified type.</summary>
    public TSelf Throws<TException>() where TException : Exception, new() { EnsureSetup().Throws<TException>(); return (TSelf)this; }
    /// <summary>Configure the method to throw the specified exception.</summary>
    public TSelf Throws(Exception exception) { EnsureSetup().Throws(exception); return (TSelf)this; }
    /// <summary>Execute a callback when the method is called.</summary>
    public TSelf Callback(Action callback) { EnsureSetup().Callback(callback); return (TSelf)this; }
    /// <summary>Transition the mock to the specified state after this setup matches.</summary>
    public TSelf TransitionsTo(string stateName) { EnsureSetup().TransitionsTo(stateName); return (TSelf)this; }
    /// <summary>Chain the next sequential behavior.</summary>
    public TSelf Then() { EnsureSetup().Then(); return (TSelf)this; }

    private ICallVerification CreateVerification()
        => MockCallVerification.Create(_engine, _memberId, _memberName, _matchers, _typeArguments);

    /// <inheritdoc />
    public void WasCalled() => CreateVerification().WasCalled();
    /// <inheritdoc />
    public void WasCalled(Times times) => CreateVerification().WasCalled(times);
    /// <inheritdoc />
    public void WasCalled(Times times, string? message) => CreateVerification().WasCalled(times, message);
    /// <inheritdoc />
    public void WasCalled(string? message) => CreateVerification().WasCalled(message);
    /// <inheritdoc />
    public void WasNeverCalled() => CreateVerification().WasNeverCalled();
    /// <inheritdoc />
    public void WasNeverCalled(string? message) => CreateVerification().WasNeverCalled(message);
}
