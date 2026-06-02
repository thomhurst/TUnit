using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Unified return type for void mock method calls. Supports both setup and verification.
/// When <c>eagerRegister</c> is true (the default for extension methods on <c>Mock&lt;T&gt;</c>),
/// the setup is registered in the constructor so that standalone calls like
/// <c>mock.Log(Arg.Any&lt;string&gt;())</c> work in strict mode without chaining.
/// When <c>eagerRegister</c> is false (used by <see cref="PropertyMockCall{TProperty}.Setter"/>),
/// registration is deferred until a setup method is explicitly called, avoiding unwanted
/// setup side-effects when the object is used purely for verification.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class VoidMockMethodCall : IVoidMethodSetup, IVoidSetupChain, ICallVerification
{
    private readonly IMockEngineAccess _engine;
    private readonly int _memberId;
    private readonly string _memberName;
    private readonly IArgumentMatcher[] _matchers;
    private readonly Type[]? _typeArguments;
    private VoidMethodSetupBuilder? _builder;
    private bool _builderInitialized;
    private object? _builderLock;

    // Kept as distinct overloads (not single optional-parameter ctors) to preserve the original
    // public/internal binary signatures for backward compatibility.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public VoidMockMethodCall(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers)
        : this(engine, memberId, memberName, matchers, eagerRegister: true, typeArguments: null)
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public VoidMockMethodCall(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers, Type[]? typeArguments)
        : this(engine, memberId, memberName, matchers, eagerRegister: true, typeArguments)
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal VoidMockMethodCall(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers, bool eagerRegister)
        : this(engine, memberId, memberName, matchers, eagerRegister, typeArguments: null)
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal VoidMockMethodCall(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers, bool eagerRegister, Type[]? typeArguments)
    {
        _engine = engine;
        _memberId = memberId;
        _memberName = memberName;
        _matchers = matchers;
        _typeArguments = typeArguments;
        if (eagerRegister)
        {
            _ = EnsureSetup();
        }
    }

    private VoidMethodSetupBuilder EnsureSetup() =>
        LazyInitializer.EnsureInitialized(ref _builder, ref _builderInitialized, ref _builderLock, () =>
        {
            var setup = new MethodSetup(_memberId, _matchers, _memberName, _typeArguments);
            _engine.AddSetup(setup);
            return new VoidMethodSetupBuilder(setup);
        })!;

    // IVoidMethodSetup implementation

    public IVoidSetupChain Returns()
    {
        EnsureSetup().Returns();
        return this;
    }

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

    public IVoidSetupChain TransitionsTo(string stateName)
    {
        EnsureSetup().TransitionsTo(stateName);
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain ReturnsRaw(object? rawValue)
    {
        EnsureSetup().ReturnsRaw(rawValue);
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IVoidSetupChain ReturnsRaw(Func<object?> factory)
    {
        EnsureSetup().ReturnsRaw(factory);
        return this;
    }

    // IVoidSetupChain implementation

    public IVoidMethodSetup Then()
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
