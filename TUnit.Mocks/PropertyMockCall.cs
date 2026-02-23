using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Matchers;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Unified return type for property mock members. Supports both setup and verification.
/// Provides setup (via <c>.Returns()</c>, <c>.Getter</c>, <c>.Setter</c>, <c>.Set()</c>)
/// and verification (via <c>.WasCalled()</c>, <c>.WasNeverCalled()</c>) on a single type.
/// Public for generated code access. Not intended for direct use.
/// </summary>
/// <typeparam name="TProperty">The property type.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct PropertyMockCall<TProperty> : ICallVerification
{
    private readonly IMockEngineAccess _engine;
    private readonly int _getterMemberId;
    private readonly int _setterMemberId;
    private readonly string _propertyName;
    private readonly bool _hasGetter;
    private readonly bool _hasSetter;

    /// <summary>Creates a new property mock call.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public PropertyMockCall(IMockEngineAccess engine, int getterMemberId, int setterMemberId,
        string propertyName, bool hasGetter, bool hasSetter)
    {
        _engine = engine;
        _getterMemberId = getterMemberId;
        _setterMemberId = setterMemberId;
        _propertyName = propertyName;
        _hasGetter = hasGetter;
        _hasSetter = hasSetter;
    }

    // --- Setup surface ---

    /// <summary>
    /// Accesses the getter setup explicitly. Creates and registers a getter <see cref="MethodSetup"/>.
    /// Note: Each access creates a new <see cref="MethodSetup"/> registration. This is intentional —
    /// it allows chaining different behaviors (e.g. <c>.Getter.Returns("x")</c> then later
    /// <c>.Getter.Returns("y")</c>) which adds a second setup that takes priority.
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
    /// Accesses the setter with any-value semantics.
    /// Returns a unified type supporting both setup (<c>.Callback()</c>, <c>.Throws()</c>)
    /// and verification (<c>.WasCalled()</c>, <c>.WasNeverCalled()</c>).
    /// </summary>
    public VoidMockMethodCall Setter
    {
        get
        {
            if (!_hasSetter)
            {
                throw new InvalidOperationException(
                    $"Property '{_propertyName}' does not have a setter.");
            }

            return new VoidMockMethodCall(_engine, _setterMemberId, $"{_propertyName} (set)",
                Array.Empty<IArgumentMatcher>());
        }
    }

    /// <summary>
    /// Accesses the setter with a specific value matcher.
    /// Returns a unified setter type supporting both setup and verification.
    /// </summary>
    public PropertySetterMockCall<TProperty> Set(Arg<TProperty> value)
    {
        if (!_hasSetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a setter.");
        }

        return new PropertySetterMockCall<TProperty>(_engine, _setterMemberId, _propertyName, value.Matcher);
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

    // --- Verify surface (delegates to getter verification) ---

    /// <inheritdoc />
    public void WasCalled(Times times)
    {
        if (!_hasGetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a getter.");
        }

        _engine.CreateVerification(_getterMemberId, $"get_{_propertyName}",
            Array.Empty<IArgumentMatcher>()).WasCalled(times);
    }

    /// <inheritdoc />
    public void WasCalled(Times times, string? message)
    {
        if (!_hasGetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a getter.");
        }

        _engine.CreateVerification(_getterMemberId, $"get_{_propertyName}",
            Array.Empty<IArgumentMatcher>()).WasCalled(times, message);
    }

    /// <inheritdoc />
    public void WasNeverCalled()
    {
        if (!_hasGetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a getter.");
        }

        _engine.CreateVerification(_getterMemberId, $"get_{_propertyName}",
            Array.Empty<IArgumentMatcher>()).WasNeverCalled();
    }

    /// <inheritdoc />
    public void WasNeverCalled(string? message)
    {
        if (!_hasGetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a getter.");
        }

        _engine.CreateVerification(_getterMemberId, $"get_{_propertyName}",
            Array.Empty<IArgumentMatcher>()).WasNeverCalled(message);
    }

    /// <inheritdoc />
    public void WasCalled()
    {
        if (!_hasGetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a getter.");
        }

        _engine.CreateVerification(_getterMemberId, $"get_{_propertyName}",
            Array.Empty<IArgumentMatcher>()).WasCalled();
    }

    /// <inheritdoc />
    public void WasCalled(string? message)
    {
        if (!_hasGetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a getter.");
        }

        _engine.CreateVerification(_getterMemberId, $"get_{_propertyName}",
            Array.Empty<IArgumentMatcher>()).WasCalled(message);
    }
}

/// <summary>
/// Unified return type for property setter calls with a specific value matcher.
/// Supports both setup (Callback, Throws) and verification (WasCalled, WasNeverCalled).
/// Eagerly registers the setup so that standalone calls (e.g., <c>mock.Name.Set(Arg.Any&lt;string&gt;())</c>)
/// work in strict mode without requiring chaining.
/// Public for generated code access. Not intended for direct use.
/// </summary>
/// <typeparam name="TProperty">The property type.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PropertySetterMockCall<TProperty> : ICallVerification
{
    private readonly IMockEngineAccess _engine;
    private readonly int _setterMemberId;
    private readonly string _propertyName;
    private readonly IArgumentMatcher _matcher;
    private VoidMethodSetupBuilder? _builder;

    /// <summary>Creates a new property setter mock call.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public PropertySetterMockCall(IMockEngineAccess engine, int setterMemberId,
        string propertyName, IArgumentMatcher matcher)
    {
        _engine = engine;
        _setterMemberId = setterMemberId;
        _propertyName = propertyName;
        _matcher = matcher;
        // Eagerly register: setter setups are commonly used without chaining
        // (e.g., mock.Name.Set(Arg.Any<string>()) to "allow" the call in strict mode).
        EnsureSetup();
    }

    private VoidMethodSetupBuilder EnsureSetup()
    {
        if (_builder is null)
        {
            var matchers = new IArgumentMatcher[] { _matcher };
            var methodSetup = new MethodSetup(_setterMemberId, matchers, $"{_propertyName} (set)");
            _engine.AddSetup(methodSetup);
            _builder = new VoidMethodSetupBuilder(methodSetup);
        }

        return _builder;
    }

    // --- Setup surface ---

    /// <summary>Execute a callback when the property setter is called with a matching value.</summary>
    public PropertySetterMockCall<TProperty> Callback(Action callback)
    {
        EnsureSetup().Callback(callback);
        return this;
    }

    /// <summary>Configure the property setter to throw when called with a matching value.</summary>
    public PropertySetterMockCall<TProperty> Throws<TException>() where TException : Exception, new()
    {
        EnsureSetup().Throws<TException>();
        return this;
    }

    /// <summary>Configure the property setter to throw a specific exception when called with a matching value.</summary>
    public PropertySetterMockCall<TProperty> Throws(Exception exception)
    {
        EnsureSetup().Throws(exception);
        return this;
    }

    // --- Verify surface ---

    /// <inheritdoc />
    public void WasCalled(Times times)
    {
        _engine.CreateVerification(_setterMemberId, $"set_{_propertyName}",
            new IArgumentMatcher[] { _matcher }).WasCalled(times);
    }

    /// <inheritdoc />
    public void WasCalled(Times times, string? message)
    {
        _engine.CreateVerification(_setterMemberId, $"set_{_propertyName}",
            new IArgumentMatcher[] { _matcher }).WasCalled(times, message);
    }

    /// <inheritdoc />
    public void WasNeverCalled()
    {
        _engine.CreateVerification(_setterMemberId, $"set_{_propertyName}",
            new IArgumentMatcher[] { _matcher }).WasNeverCalled();
    }

    /// <inheritdoc />
    public void WasNeverCalled(string? message)
    {
        _engine.CreateVerification(_setterMemberId, $"set_{_propertyName}",
            new IArgumentMatcher[] { _matcher }).WasNeverCalled(message);
    }

    /// <inheritdoc />
    public void WasCalled()
    {
        _engine.CreateVerification(_setterMemberId, $"set_{_propertyName}",
            new IArgumentMatcher[] { _matcher }).WasCalled();
    }

    /// <inheritdoc />
    public void WasCalled(string? message)
    {
        _engine.CreateVerification(_setterMemberId, $"set_{_propertyName}",
            new IArgumentMatcher[] { _matcher }).WasCalled(message);
    }
}
