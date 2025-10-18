using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for property assertions on objects.
/// Provides HasProperty sugar syntax as a simpler alternative to .Member() for common cases.
/// </summary>
public static class PropertyAssertionExtensions
{
    /// <summary>
    /// Asserts that an object has a property with the expected value (simple equality check).
    /// This is sugar syntax for the common case of checking property equality.
    /// Returns back to the parent object context for further chaining.
    /// Example: await Assert.That(obj).HasProperty(x => x.Name, "expected").And.HasProperty(x => x.Age, 25);
    /// </summary>
    public static MemberAssertionResult<TObject> HasProperty<TObject, TProperty>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, TProperty>> propertySelector,
        TProperty expectedValue,
        [CallerArgumentExpression(nameof(expectedValue))] string? expression = null)
    {
        var memberPath = GetMemberPath(propertySelector);
        source.Context.ExpressionBuilder.Append($".HasProperty(x => x.{memberPath}, {expression})");

        // Use the existing .Member() infrastructure
        return source.Member(propertySelector, prop => prop.IsEqualTo(expectedValue));
    }

    /// <summary>
    /// Returns a fluent property assertion builder that allows chaining assertion methods.
    /// Provides better type inference and readability than .Member() for simple property checks.
    /// Example: await Assert.That(obj).HasProperty(x => x.Name).IsEqualTo("expected");
    /// Example: await Assert.That(obj).HasProperty(x => x.Value).IsNotNull();
    /// </summary>
    public static PropertyAssertion<TObject, TProperty> HasProperty<TObject, TProperty>(
        this IAssertionSource<TObject> source,
        Expression<Func<TObject, TProperty>> propertySelector)
    {
        var parentContext = source.Context;
        var memberPath = GetMemberPath(propertySelector);

        parentContext.ExpressionBuilder.Append($".HasProperty(x => x.{memberPath})");

        // Map to property context
        var propertyContext = parentContext.Map<TProperty>(obj =>
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }

            var compiled = propertySelector.Compile();
            return compiled(obj);
        });

        return new PropertyAssertion<TObject, TProperty>(parentContext, propertyContext, memberPath);
    }

    /// <summary>
    /// Extracts the member path from a member selector expression.
    /// </summary>
    private static string GetMemberPath<TObject, TMember>(Expression<Func<TObject, TMember>> selector)
    {
        return selector.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression member } => member.Member.Name,
            _ => "?"
        };
    }
}
