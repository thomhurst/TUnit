using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Matchers;

namespace TUnit.Mocks.Setup;

/// <summary>
/// Lightweight accessor for property setup. Returns from generated class members like
/// <c>mock.Setup.Name</c>. Lazily creates <see cref="MethodSetup"/> instances on demand.
/// </summary>
/// <typeparam name="TProperty">The property type.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct PropertySetupAccessor<TProperty>
{
    private readonly IMockEngineAccess _engine;
    private readonly int _getterMemberId;
    private readonly int _setterMemberId;
    private readonly string _propertyName;
    private readonly bool _hasGetter;
    private readonly bool _hasSetter;

    /// <summary>Creates a new property setup accessor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public PropertySetupAccessor(IMockEngineAccess engine, int getterMemberId, int setterMemberId,
        string propertyName, bool hasGetter, bool hasSetter)
    {
        _engine = engine;
        _getterMemberId = getterMemberId;
        _setterMemberId = setterMemberId;
        _propertyName = propertyName;
        _hasGetter = hasGetter;
        _hasSetter = hasSetter;
    }

    /// <summary>
    /// Accesses the getter setup explicitly. Creates and registers a getter <see cref="MethodSetup"/>.
    /// </summary>
    public IPropertySetup<TProperty> Getter
    {
        get
        {
            if (!_hasGetter)
            {
                throw new InvalidOperationException(
                    $"Property '{_propertyName}' does not have a getter.");
            }

            var matchers = Array.Empty<IArgumentMatcher>();
            var methodSetup = new MethodSetup(_getterMemberId, matchers, $"{_propertyName} (get)");
            _engine.AddSetup(methodSetup);
            return new PropertySetupBuilder<TProperty>(methodSetup);
        }
    }

    /// <summary>
    /// Accesses the setter setup with any-value semantics.
    /// Creates and registers a setter <see cref="MethodSetup"/> matching any argument.
    /// </summary>
    public IPropertySetterSetup Setter
    {
        get
        {
            if (!_hasSetter)
            {
                throw new InvalidOperationException(
                    $"Property '{_propertyName}' does not have a setter.");
            }

            var matchers = Array.Empty<IArgumentMatcher>();
            var methodSetup = new MethodSetup(_setterMemberId, matchers, $"{_propertyName} (set)");
            _engine.AddSetup(methodSetup);
            return new PropertySetterSetupBuilder(methodSetup);
        }
    }

    /// <summary>
    /// Accesses the setter setup with a specific value matcher.
    /// Creates and registers a setter <see cref="MethodSetup"/> matching the specified value.
    /// </summary>
    public IPropertySetterSetup Set(Arg<TProperty> value)
    {
        if (!_hasSetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a setter.");
        }

        var matchers = new IArgumentMatcher[] { value.Matcher };
        var methodSetup = new MethodSetup(_setterMemberId, matchers, $"{_propertyName} (set)");
        _engine.AddSetup(methodSetup);
        return new PropertySetterSetupBuilder(methodSetup);
    }

    // Convenience shortcuts that delegate to Getter

    /// <summary>Configure a fixed return value for the property getter.</summary>
    public ISetupChain<TProperty> Returns(TProperty value) => Getter.Returns(value);

    /// <summary>Configure a computed return value for the property getter.</summary>
    public ISetupChain<TProperty> Returns(Func<TProperty> factory) => Getter.Returns(factory);

    /// <summary>Configure sequential return values for the property getter.</summary>
    public ISetupChain<TProperty> ReturnsSequentially(params TProperty[] values) => Getter.ReturnsSequentially(values);

    /// <summary>Configure the property getter to throw.</summary>
    public ISetupChain<TProperty> Throws<TException>() where TException : Exception, new() => Getter.Throws<TException>();

    /// <summary>Configure the property getter to throw a specific exception.</summary>
    public ISetupChain<TProperty> Throws(Exception exception) => Getter.Throws(exception);

    /// <summary>Execute a callback when the property getter is called.</summary>
    public ISetupChain<TProperty> Callback(Action callback) => Getter.Callback(callback);
}
