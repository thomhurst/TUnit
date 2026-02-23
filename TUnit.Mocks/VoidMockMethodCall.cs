using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Unified return type for void mock method calls. Supports both setup and verification.
/// Lazily registers the method setup only when a setup method is called.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class VoidMockMethodCall : IVoidMethodSetup, IVoidSetupChain, ICallVerification
{
    private readonly IMockEngineAccess _engine;
    private readonly int _memberId;
    private readonly string _memberName;
    private readonly IArgumentMatcher[] _matchers;
    private VoidMethodSetupBuilder? _builder;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public VoidMockMethodCall(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers)
    {
        _engine = engine;
        _memberId = memberId;
        _memberName = memberName;
        _matchers = matchers;
        // Eagerly register: void methods are commonly used without chaining
        // (e.g., mock.Log(Arg.Any<string>()) to "allow" the call in strict mode).
        EnsureSetup();
    }

    private VoidMethodSetupBuilder EnsureSetup()
    {
        if (_builder is null)
        {
            var setup = new MethodSetup(_memberId, _matchers, _memberName);
            _engine.AddSetup(setup);
            _builder = new VoidMethodSetupBuilder(setup);
        }

        return _builder;
    }

    // IVoidMethodSetup implementation

    public IVoidSetupChain Throws<TException>() where TException : Exception, new()
    {
        EnsureSetup().Throws<TException>();
        return this;
    }

    public IVoidSetupChain Throws(Exception exception)
    {
        EnsureSetup().Throws(exception);
        return this;
    }

    public IVoidSetupChain Callback(Action callback)
    {
        EnsureSetup().Callback(callback);
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Callback(Action<object?[]> callback)
    {
        EnsureSetup().Callback(callback);
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain Throws(Func<object?[], Exception> exceptionFactory)
    {
        EnsureSetup().Throws(exceptionFactory);
        return this;
    }

    public IVoidSetupChain Raises(string eventName, object? args = null)
    {
        EnsureSetup().Raises(eventName, args);
        return this;
    }

    public IVoidSetupChain SetsOutParameter(int paramIndex, object? value)
    {
        EnsureSetup().SetsOutParameter(paramIndex, value);
        return this;
    }

    public IVoidSetupChain TransitionsTo(string stateName)
    {
        EnsureSetup().TransitionsTo(stateName);
        return this;
    }

    // IVoidSetupChain implementation

    public IVoidMethodSetup Then()
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
