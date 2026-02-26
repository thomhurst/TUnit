using System.ComponentModel;

namespace TUnit.Mocks.Setup;

/// <summary>
/// Concrete implementation of setup builder for property setters.
/// Delegates to a <see cref="VoidMethodSetupBuilder"/> for all behavior.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PropertySetterSetupBuilder : IPropertySetterSetup, IVoidSetupChain
{
    private readonly VoidMethodSetupBuilder _inner;

    public PropertySetterSetupBuilder(MethodSetup setup)
    {
        _inner = new VoidMethodSetupBuilder(setup);
    }

    public IVoidSetupChain Throws<TException>() where TException : Exception, new() => _inner.Throws<TException>();
    public IVoidSetupChain Throws(Exception exception) => _inner.Throws(exception);
    public IVoidSetupChain Callback(Action callback) => _inner.Callback(callback);
    public IVoidSetupChain Callback(Action<object?[]> callback) => _inner.Callback(callback);
    public IVoidSetupChain Throws(Func<object?[], Exception> exceptionFactory) => _inner.Throws(exceptionFactory);
    public IVoidSetupChain Raises(string eventName, object? args = null) => _inner.Raises(eventName, args);
    public IVoidSetupChain SetsOutParameter(int paramIndex, object? value) => _inner.SetsOutParameter(paramIndex, value);
    public IVoidSetupChain TransitionsTo(string stateName) => _inner.TransitionsTo(stateName);
    public IVoidMethodSetup Then() => _inner.Then();
}
