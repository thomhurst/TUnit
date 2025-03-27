using System.Text;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public class ConvertedDelegateSource<TFromType, TToType> : IValueSource<TToType?> where TToType : Exception
{
    public ConvertedDelegateSource(IDelegateSource source)
    {
        var convertToAssertCondition = new ConvertExceptionToValueAssertCondition<TToType>();
        
        ActualExpression = source.ActualExpression;
        Assertions = new Stack<BaseAssertCondition>([new DelegateConversionAssertionCondition<TToType>(source, convertToAssertCondition)]);
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

    private static async ValueTask<AssertionData> ConvertAsync(IDelegateSource valueSource, ConvertExceptionToValueAssertCondition<TToType> convertToAssertCondition)
    {
        var invokableAssertionBuilder = valueSource.RegisterAssertion(convertToAssertCondition, []);
        
        return await invokableAssertionBuilder.ProcessAssertionsAsync(assertionData => 
            Task.FromResult(assertionData with { Result = convertToAssertCondition.ConvertedExceptionValue, End = DateTimeOffset.Now }));

    }
}