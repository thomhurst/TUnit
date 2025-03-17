using System.Text;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public class ConvertedValueSource<TFromType, TToType>(IValueSource<TFromType> source, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition) : IValueSource<TToType?>
{
    public string? ActualExpression { get; } = source.ActualExpression;
    public Stack<BaseAssertCondition> Assertions { get; } = new([new NoOpAssertionCondition<TToType>()]);
    public ValueTask<AssertionData> AssertionDataTask { get; } = ConvertAsync(source, convertToAssertCondition);

    public StringBuilder ExpressionBuilder { get; } = source.ExpressionBuilder;

    public ISource AppendExpression(string expression)
    {
        if (!string.IsNullOrEmpty(expression))
        {
            ExpressionBuilder.Append('.');
            ExpressionBuilder.Append(expression);
        }

        return this;
    }

    public ISource WithAssertion(BaseAssertCondition assertCondition)
    {
        Assertions.Push(assertCondition);
        return this;
    }

    private static async ValueTask<AssertionData> ConvertAsync(IValueSource<TFromType> valueSource, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition)
    {
        var invokableAssertionBuilder = valueSource.RegisterAssertion(convertToAssertCondition, []);
        
        return await invokableAssertionBuilder.ProcessAssertionsAsync(assertionData => 
            Task.FromResult(assertionData with { Result = convertToAssertCondition.ConvertedValue, End = DateTimeOffset.Now }));

    }
}