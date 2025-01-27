using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class SourceExtensions
{
    public static InvokableValueAssertionBuilder<TActual> RegisterAssertion<TActual>(this IValueSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition, string[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        if (!string.IsNullOrEmpty(caller))
        {
            source.AppendExpression(BuildExpression(caller, argumentExpressions));
        }
        
        var invokeableAssertionBuilder = source.WithAssertion(assertCondition);

        if (invokeableAssertionBuilder is InvokableValueAssertionBuilder<TActual> invokableValueAssertionBuilder)
        {
            return invokableValueAssertionBuilder;
        }
        
        if(invokeableAssertionBuilder is InvokableAssertionBuilder<TActual> invokableAssertionBuilder)
        {
            return new InvokableValueAssertionBuilder<TActual>(invokableAssertionBuilder);
        }
        
        return new InvokableValueAssertionBuilder<TActual>(new InvokableAssertionBuilder<TActual>(invokeableAssertionBuilder));
    }

    public static InvokableDelegateAssertionBuilder RegisterAssertion<TActual>(this IDelegateSource delegateSource,
        BaseAssertCondition<TActual> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        if (!string.IsNullOrEmpty(caller))
        {
            delegateSource.AppendExpression(BuildExpression(caller, argumentExpressions));
        }
        
        var source = delegateSource.WithAssertion(assertCondition);

        if (source is InvokableDelegateAssertionBuilder unTypedInvokableDelegateAssertionBuilder)
        {
            return unTypedInvokableDelegateAssertionBuilder;
        }

        if (source is InvokableAssertionBuilder<object?> unTypedInvokableAssertionBuilder)
        {
            return new InvokableDelegateAssertionBuilder(unTypedInvokableAssertionBuilder);
        }

        return new InvokableDelegateAssertionBuilder(new InvokableAssertionBuilder<object?>(source));
    }

    private static string BuildExpression(string caller, string?[] argumentExpressions)
    {
        var assertionBuilder = new StringBuilder();

        argumentExpressions = argumentExpressions.OfType<string>().ToArray();
        
        assertionBuilder.Append(caller)
            .Append('(');
        
        for (var index = 0; index < argumentExpressions.Length; index++)
        {
            var argumentExpression = argumentExpressions[index];

            if (string.IsNullOrEmpty(argumentExpression))
            {
                continue;
            }
            
            assertionBuilder.Append(argumentExpression!);

            if (index < argumentExpressions.Length - 1)
            {
                assertionBuilder.Append(',');
                assertionBuilder.Append(' ');
            }
        }

        return assertionBuilder.Append(')').ToString();
    }
}