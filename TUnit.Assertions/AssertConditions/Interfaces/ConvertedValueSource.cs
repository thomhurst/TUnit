using System.Text;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public class ConvertedValueSource<TFromType, TToType> : IValueSource<TToType?>
{
    public ConvertedValueSource(IValueSource<TFromType> source, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition)
    {
        ActualExpression = source.ActualExpression;
        Assertions = new Stack<BaseAssertCondition>([new ValueConversionAssertionCondition<TFromType, TToType>(source, convertToAssertCondition)]);
        LazyAssertionData = source.LazyAssertionData.WithConversion(source, convertToAssertCondition);
        ExpressionBuilder = source.ExpressionBuilder;
    }

    public string? ActualExpression { get; }
    public Stack<BaseAssertCondition> Assertions { get; }
    public LazyAssertionData LazyAssertionData { get; }

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
}
