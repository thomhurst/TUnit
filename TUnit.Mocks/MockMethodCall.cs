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
    private readonly Lazy<MethodSetupBuilder<TReturn>> _lazyBuilder;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public MockMethodCall(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers)
    {
        _engine = engine;
        _memberId = memberId;
        _memberName = memberName;
        _matchers = matchers;
        _lazyBuilder = new Lazy<MethodSetupBuilder<TReturn>>(() =>
        {
            var setup = new MethodSetup(_memberId, _matchers, _memberName);
            _engine.AddSetup(setup);
            return new MethodSetupBuilder<TReturn>(setup);
        });
    }

    private MethodSetupBuilder<TReturn> EnsureSetup() => _lazyBuilder.Value;

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

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Callback(Action<object?[]> callback)
    {
        EnsureSetup().Callback(callback);
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Returns(Func<object?[], TReturn> factory)
    {
        EnsureSetup().Returns(factory);
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ISetupChain<TReturn> Throws(Func<object?[], Exception> exceptionFactory)
    {
        EnsureSetup().Throws(exceptionFactory);
        return this;
    }

    public ISetupChain<TReturn> Raises(string eventName, object? args = null)
    {
        EnsureSetup().Raises(eventName, args);
        return this;
    }

    public ISetupChain<TReturn> SetsOutParameter(int paramIndex, object? value)
    {
        EnsureSetup().SetsOutParameter(paramIndex, value);
        return this;
    }

    public ISetupChain<TReturn> TransitionsTo(string stateName)
    {
        EnsureSetup().TransitionsTo(stateName);
        return this;
    }

    // ISetupChain<TReturn> implementation

    public IMethodSetup<TReturn> Then()
    {
        EnsureSetup().Then();
        return this;
    }

    // ICallVerification implementation

    public void WasCalled(Times times)
    {
        _engine.CreateVerification(_memberId, _memberName, _matchers).WasCalled(times);
    }

    public void WasCalled(Times times, string? message)
    {
        _engine.CreateVerification(_memberId, _memberName, _matchers).WasCalled(times, message);
    }

    public void WasNeverCalled()
    {
        _engine.CreateVerification(_memberId, _memberName, _matchers).WasNeverCalled();
    }

    public void WasNeverCalled(string? message)
    {
        _engine.CreateVerification(_memberId, _memberName, _matchers).WasNeverCalled(message);
    }

    public void WasCalled()
    {
        _engine.CreateVerification(_memberId, _memberName, _matchers).WasCalled();
    }

    public void WasCalled(string? message)
    {
        _engine.CreateVerification(_memberId, _memberName, _matchers).WasCalled(message);
    }
}
