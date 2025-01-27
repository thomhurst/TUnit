using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class SourceExtensions
{
    public static InvokableValueAssertionBuilder<TActual> RegisterAssertion<TActual>(this IValueSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition, string[] argumentExpressions, [CallerMemberName] string caller = "")
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
        
        return new InvokableValueAssertionBuilder<TActual>((InvokableAssertionBuilder<TActual>)invokeableAssertionBuilder);
    }

    public static InvokableDelegateAssertionBuilder<TActual> RegisterAssertion<TActual>(this IDelegateSource source,
        BaseAssertCondition<TActual> assertCondition, string[] argumentExpressions, [CallerMemberName] string caller = "")
    {
        if (!string.IsNullOrEmpty(caller))
        {
            source.AppendExpression(BuildExpression(caller, argumentExpressions));
        }
        
        var invokeableAssertionBuilder = source.WithAssertion(assertCondition);

        if (invokeableAssertionBuilder is InvokableDelegateAssertionBuilder<TActual> invokableDelegateAssertionBuilder)
        {
            return invokableDelegateAssertionBuilder;
        }
        
        return new InvokableDelegateAssertionBuilder<TActual>((InvokableAssertionBuilder<TActual>)invokeableAssertionBuilder);
    }

    private static string BuildExpression(string caller, string[] argumentExpressions)
    {
        var assertionBuilder = new StringBuilder();
        assertionBuilder.Append('.')
            .Append(caller)
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