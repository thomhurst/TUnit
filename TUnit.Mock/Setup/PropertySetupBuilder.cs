using System.ComponentModel;
using TUnit.Mock.Setup.Behaviors;

namespace TUnit.Mock.Setup;

/// <summary>
/// Concrete implementation of setup builder for property getters.
/// Delegates to a <see cref="MethodSetupBuilder{TProperty}"/> for all behavior.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PropertySetupBuilder<TProperty> : IPropertySetup<TProperty>, ISetupChain<TProperty>
{
    private readonly MethodSetupBuilder<TProperty> _inner;

    public PropertySetupBuilder(MethodSetup setup)
    {
        _inner = new MethodSetupBuilder<TProperty>(setup);
    }

    public ISetupChain<TProperty> Returns(TProperty value) => _inner.Returns(value);
    public ISetupChain<TProperty> Returns(Func<TProperty> factory) => _inner.Returns(factory);
    public ISetupChain<TProperty> ReturnsSequentially(params TProperty[] values) => _inner.ReturnsSequentially(values);
    public ISetupChain<TProperty> Throws<TException>() where TException : Exception, new() => _inner.Throws<TException>();
    public ISetupChain<TProperty> Throws(Exception exception) => _inner.Throws(exception);
    public ISetupChain<TProperty> Callback(Action callback) => _inner.Callback(callback);
    public ISetupChain<TProperty> Callback(Action<object?[]> callback) => _inner.Callback(callback);
    public ISetupChain<TProperty> Returns(Func<object?[], TProperty> factory) => _inner.Returns(factory);
    public ISetupChain<TProperty> Throws(Func<object?[], Exception> exceptionFactory) => _inner.Throws(exceptionFactory);
    public ISetupChain<TProperty> Raises(string eventName, object? args = null) => _inner.Raises(eventName, args);
    public ISetupChain<TProperty> SetsOutParameter(int paramIndex, object? value) => _inner.SetsOutParameter(paramIndex, value);
    public IMethodSetup<TProperty> Then() => _inner.Then();
}
