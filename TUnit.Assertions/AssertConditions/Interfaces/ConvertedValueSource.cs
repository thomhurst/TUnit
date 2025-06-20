using System.Text;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public class ConvertedValueSource<TFromType, TToType> : IValueSource<TToType?>
{
    public ConvertedValueSource(IValueSource<TFromType> source, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition)
    {
        ActualExpression = source.ActualExpression;
        Assertions = new Stack<BaseAssertCondition>([new ValueConversionAssertionCondition<TFromType, TToType>(source, convertToAssertCondition)]);
        AssertionDataTask = ConvertAsync(source, convertToAssertCondition);
        ExpressionBuilder = source.ExpressionBuilder;
    }

    public string? ActualExpression { get; }
    public Stack<BaseAssertCondition> Assertions { get; }
    public ValueTask<AssertionData> AssertionDataTask { get; }

    public StringBuilder ExpressionBuilder { get; }

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
        var invokableAssertionBuilder = valueSource.RegisterAssertion(convertToAssertCondition, [], null);
        
        return await invokableAssertionBuilder.ProcessAssertionsAsync(assertionData => 
            Task.FromResult(assertionData with { Result = convertToAssertCondition.ConvertedValue, End = DateTimeOffset.Now }));

    }
}