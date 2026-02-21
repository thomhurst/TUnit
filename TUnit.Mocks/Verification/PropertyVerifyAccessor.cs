using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Matchers;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Lightweight accessor for property verification. Returns from generated class members like
/// <c>mock.Verify.Name</c>. Lazily creates verification builders on demand.
/// Implements <see cref="ICallVerification"/> to allow <c>mock.Verify.Name.WasCalled(...)</c> directly
/// (delegates to getter verification).
/// </summary>
/// <typeparam name="TProperty">The property type.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct PropertyVerifyAccessor<TProperty> : ICallVerification
{
    private readonly IMockEngineAccess _engine;
    private readonly int _getterMemberId;
    private readonly int _setterMemberId;
    private readonly string _propertyName;
    private readonly bool _hasGetter;
    private readonly bool _hasSetter;

    /// <summary>Creates a new property verify accessor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public PropertyVerifyAccessor(IMockEngineAccess engine, int getterMemberId, int setterMemberId,
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
    /// Accesses the getter verification explicitly.
    /// </summary>
    public ICallVerification Getter
    {
        get
        {
            if (!_hasGetter)
            {
                throw new InvalidOperationException(
                    $"Property '{_propertyName}' does not have a getter.");
            }

            return _engine.CreateVerification(_getterMemberId, $"get_{_propertyName}",
                Array.Empty<IArgumentMatcher>());
        }
    }

    /// <summary>
    /// Accesses the setter verification with any-value semantics.
    /// </summary>
    public ICallVerification Setter
    {
        get
        {
            if (!_hasSetter)
            {
                throw new InvalidOperationException(
                    $"Property '{_propertyName}' does not have a setter.");
            }

            return _engine.CreateVerification(_setterMemberId, $"set_{_propertyName}",
                Array.Empty<IArgumentMatcher>());
        }
    }

    /// <summary>
    /// Accesses the setter verification with a specific value matcher.
    /// </summary>
    public ICallVerification Set(Arg<TProperty> value)
    {
        if (!_hasSetter)
        {
            throw new InvalidOperationException(
                $"Property '{_propertyName}' does not have a setter.");
        }

        return _engine.CreateVerification(_setterMemberId, $"set_{_propertyName}",
            new IArgumentMatcher[] { value.Matcher });
    }

    // ICallVerification implementation — delegates to Getter

    /// <inheritdoc />
    public void WasCalled(Times times) => Getter.WasCalled(times);

    /// <inheritdoc />
    public void WasCalled(Times times, string? message) => Getter.WasCalled(times, message);

    /// <inheritdoc />
    public void WasNeverCalled() => Getter.WasNeverCalled();

    /// <inheritdoc />
    public void WasNeverCalled(string? message) => Getter.WasNeverCalled(message);

    /// <inheritdoc />
    public void WasCalled() => Getter.WasCalled();

    /// <inheritdoc />
    public void WasCalled(string? message) => Getter.WasCalled(message);
}
