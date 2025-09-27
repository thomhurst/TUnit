using System.Text;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public class ConvertedDelegateSource<TToType> : IValueSource<TToType?> where TToType : Exception
{
    public ConvertedDelegateSource(IDelegateSource source)
    {
        var convertToAssertCondition = new ConvertExceptionToValueAssertCondition<TToType>();

        ActualExpression = source.ActualExpression;
        Assertions = new Stack<BaseAssertCondition>([new DelegateConversionAssertionCondition<TToType>(source, (BaseAssertCondition<object?>) source.Assertions.Peek())]);
        LazyAssertionData = source.LazyAssertionData.WithExceptionAsValue(source, convertToAssertCondition);
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
