using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class SourceExtensions
{
    public static InvokableValueAssertion<TActual> RegisterAssertion<TActual>(this IValueSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        if (!string.IsNullOrEmpty(caller))
        {
            source.AppendExpression(BuildExpression(caller, argumentExpressions));
        }

        var invokeableAssertion = source.WithAssertion(assertCondition);

        if (invokeableAssertion is InvokableValueAssertion<TActual> invokableValueAssertion)
        {
            return invokableValueAssertion;
        }

        if (invokeableAssertion is InvokableAssertion<TActual> invokableAssertion)
        {
            return new InvokableValueAssertion<TActual>(invokableAssertion);
        }

        return new InvokableValueAssertion<TActual>(new InvokableAssertion<TActual>(invokeableAssertion));
    }

    public static InvokableValueAssertion<TToType> RegisterConversionAssertion<TFromType, TToType>(this IValueSource<TFromType> source,
        ConvertToAssertCondition<TFromType, TToType> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        return new ConvertedValueAssertionBuilder<TFromType, TToType>(source, assertCondition);
    }

    public static InvokableDelegateAssertion RegisterAssertion<TActual>(this IDelegateSource delegateSource,
        BaseAssertCondition<TActual> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        if (!string.IsNullOrEmpty(caller))
        {
            delegateSource.AppendExpression(BuildExpression(caller, argumentExpressions));
        }

        var source = delegateSource.WithAssertion(assertCondition);

        if (source is InvokableDelegateAssertion unTypedInvokableDelegateAssertion)
        {
            return unTypedInvokableDelegateAssertion;
        }

        if (source is InvokableAssertion<object?> unTypedInvokableAssertion)
        {
            return new InvokableDelegateAssertion(unTypedInvokableAssertion);
        }

        return new InvokableDelegateAssertion(new InvokableAssertion<object?>(source));
    }

    public static InvokableValueAssertion<TToType> RegisterConversionAssertion<TToType>(this IDelegateSource source) where TToType : Exception
    {
        return new ConvertedDelegateAssertionBuilder<TToType>(source);
    }

    private static string BuildExpression(string? caller, string?[] argumentExpressions)
    {
        var assertionBuilder = new StringBuilder();

        argumentExpressions = argumentExpressions.OfType<string>().ToArray();

        if (caller is not null)
        {
            assertionBuilder.Append(caller);
        }

        assertionBuilder.Append('(');

        for (var index = 0; index < argumentExpressions.Length; index++)
        {
            var argumentExpression = argumentExpressions[index];

            if (string.IsNullOrEmpty(argumentExpression))
            {
                continue;
            }

            assertionBuilder.Append(argumentExpression);

            if (index < argumentExpressions.Length - 1)
            {
                assertionBuilder.Append(',');
                assertionBuilder.Append(' ');
            }
        }

        return assertionBuilder.Append(')').ToString();
    }
}
