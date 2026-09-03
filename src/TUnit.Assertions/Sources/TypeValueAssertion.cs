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

    /// <summary>
    /// Asserts that the represented type is not assignable to <typeparamref name="TTarget"/>.
    /// </summary>
    public new IsNotAssignableToAssertion<TTarget, Type> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, Type>(Context, useRepresentedType: true);
    }

    /// <summary>
    /// Asserts that <typeparamref name="TSource"/> is assignable to the represented type.
    /// </summary>
    public new IsAssignableFromAssertion<TSource, Type> IsAssignableFrom<TSource>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TSource).Name}>()");
        return new IsAssignableFromAssertion<TSource, Type>(Context, useRepresentedType: true);
    }

    /// <summary>
    /// Asserts that <typeparamref name="TSource"/> is not assignable to the represented type.
    /// </summary>
    public new IsNotAssignableFromAssertion<TSource, Type> IsNotAssignableFrom<TSource>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TSource).Name}>()");
        return new IsNotAssignableFromAssertion<TSource, Type>(Context, useRepresentedType: true);
    }
}
