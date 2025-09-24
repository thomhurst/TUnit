using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class SourceExtensions
{
    public static AssertionBuilder<TActual> RegisterAssertion<TActual>(this IValueSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        if (!string.IsNullOrEmpty(caller))
        {
            source.AppendExpression(BuildExpression(caller, argumentExpressions));
        }

        source.WithAssertion(assertCondition);

        if (source is AssertionBuilder<TActual> assertionBuilder)
        {
            return assertionBuilder;
        }

        // This shouldn't happen with our new architecture
        throw new InvalidOperationException("Source is not an AssertionBuilder");
    }

    public static AssertionBuilder<TToType> RegisterConversionAssertion<TFromType, TToType>(this IValueSource<TFromType> source,
        ConvertToAssertCondition<TFromType, TToType> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        // For now, return a simple AssertionBuilder with the conversion
        // In a real implementation, this would handle the type conversion properly
        return new AssertionBuilder<TToType>(default(TToType)!, source.ActualExpression);
    }

    public static AssertionBuilder<object?> RegisterAssertion<TActual>(this IDelegateSource delegateSource,
        BaseAssertCondition<TActual> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        if (!string.IsNullOrEmpty(caller))
        {
            delegateSource.AppendExpression(BuildExpression(caller, argumentExpressions));
        }

        delegateSource.WithAssertion(assertCondition);

        if (delegateSource is AssertionBuilder<object?> delegateAssertionBuilder)
        {
            return delegateAssertionBuilder;
        }

        throw new InvalidOperationException("Source is not an AssertionBuilder<object?>");
    }

    public static AssertionBuilder<TToType> RegisterConversionAssertion<TToType>(this IDelegateSource source) where TToType : Exception
    {
        // Simplified for now
        return new AssertionBuilder<TToType>(default(TToType)!, source.ActualExpression);
    }

    private static string BuildExpression(string? caller, string?[] arguments)
    {
        var sb = new StringBuilder();
        sb.Append(caller);
        sb.Append('(');

        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            sb.Append(argument);

            if (index < arguments.Length - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(')');
        return sb.ToString();
    }
}