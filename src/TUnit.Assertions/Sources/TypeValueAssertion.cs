using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for represented types.
/// </summary>
public sealed class TypeValueAssertion : ValueAssertion<Type>
{
    public TypeValueAssertion(Type? value, string? expression)
        : base(value, expression)
    {
    }

    /// <summary>
    /// Asserts that the represented type is assignable to <typeparamref name="TTarget"/>.
    /// The assertion retains the represented <see cref="Type"/> for awaiting and chaining.
    /// </summary>
    public new TypeIsAssignableToAssertion<TTarget> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new TypeIsAssignableToAssertion<TTarget>(Context);
    }
}
