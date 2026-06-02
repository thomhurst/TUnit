using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Unified return type for non-void mock method calls. Supports both setup and verification.
/// Lazily registers the method setup only when a setup method (e.g. <c>.Returns()</c>) is called.
/// Unlike <see cref="VoidMockMethodCall"/>, non-void calls are NOT eagerly registered because
/// a non-void method without a configured return value is rarely useful. To explicitly allow
/// a non-void call in strict mode without configuring a return value, chain <c>.Returns(default!)</c>.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MockMethodCall<TReturn> : IMethodSetup<TReturn>, ISetupChain<TReturn>, ICallVerification
{
    private readonly IMockEngineAccess _engine;
    private readonly int _memberId;
    private readonly string _memberName;
    private readonly IArgumentMatcher[] _matchers;
    private readonly Type[]? _typeArguments;
    private MethodSetupBuilder<TReturn>? _builder;
    private bool _builderInitialized;
    private object? _builderLock;

    // Kept as a distinct overload (not a single optional-parameter ctor) to preserve the original
    // public binary signature for backward compatibility.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MockMethodCall(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers)
        : this(engine, memberId, memberName, matchers, null)
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public MockMethodCall(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers, Type[]? typeArguments)
    {
        _engine = engine;
        _memberId = memberId;
        _memberName = memberName;
        _matchers = matchers;
        _typeArguments = typeArguments;
    }

    private MethodSetupBuilder<TReturn> EnsureSetup() =>
        LazyInitializer.EnsureInitialized(ref _builder, ref _builderInitialized, ref _builderLock, () =>
        {
            var setup = new MethodSetup(_memberId, _matchers, _memberName, _typeArguments);
            _engine.AddSetup(setup);
            return new MethodSetupBuilder<TReturn>(setup);
        })!;

    // IMethodSetup<TReturn> implementation

    public ISetupChain<TReturn> Returns(TReturn value)
    {
        EnsureSetup().Returns(value);
        return this;
    }

    public ISetupChain<TReturn> Returns(Func<TReturn> factory)
    {
        EnsureSetup().Returns(factory);
        return this;
    }

    public ISetupChain<TReturn> ReturnsSequentially(params TReturn[] values)
    {
        EnsureSetup().ReturnsSequentially(values);
        return this;
    }

    public ISetupChain<TReturn> Throws<TException>() where TException : Exception, new()
    {
        EnsureSetup().Throws<TException>();
        return this;
    }

    public ISetupChain<TReturn> Throws(Exception exception)
    {
        EnsureSetup().Throws(exception);
        return this;
    }

    public ISetupChain<TReturn> Callback(Action callback)
    {
        EnsureSetup().Callback(callback);
        return this;
    }

    public ISetupChain<TReturn> TransitionsTo(string stateName)
    {
        EnsureSetup().TransitionsTo(stateName);
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> ReturnsRaw(object? rawValue)
    {
        EnsureSetup().ReturnsRaw(rawValue);
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> ReturnsRaw(Func<object?> factory)
    {
        EnsureSetup().ReturnsRaw(factory);
        return this;
    }

    // ISetupChain<TReturn> implementation

    public IMethodSetup<TReturn> Then()
    {
        EnsureSetup().Then();
        return this;
    }

    // ICallVerification implementation

    // Non-generic calls use the public engine surface unchanged; generic calls route through the
    // internal type-argument-aware factory (the engine is always a MockEngine, which implements it).
    private ICallVerification CreateVerification()
        => _typeArguments is null
            ? _engine.CreateVerification(_memberId, _memberName, _matchers)
            : ((ITypeArgumentVerificationFactory)_engine).CreateVerification(_memberId, _memberName, _matchers, _typeArguments);

    public void WasCalled(Times times)
    {
        CreateVerification().WasCalled(times);
    }

    public void WasCalled(Times times, string? message)
    {
        CreateVerification().WasCalled(times, message);
    }

    public void WasNeverCalled()
    {
        CreateVerification().WasNeverCalled();
    }

    public void WasNeverCalled(string? message)
    {
        CreateVerification().WasNeverCalled(message);
    }

    public void WasCalled()
    {
        CreateVerification().WasCalled();
    }

    public void WasCalled(string? message)
    {
        CreateVerification().WasCalled(message);
    }
}
