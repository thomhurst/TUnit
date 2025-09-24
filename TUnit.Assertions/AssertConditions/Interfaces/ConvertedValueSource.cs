using System.Text;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public class ConvertedValueSource<TFromType, TToType> : IValueSource<TToType?>
{
    public ConvertedValueSource(IValueSource<TFromType> source, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition)
    {
        ActualExpression = source.ActualExpression;
        _assertions.Add(new ValueConversionAssertionCondition<TFromType, TToType>(source, convertToAssertCondition));
        AssertionDataTask = ConvertAsync(source, convertToAssertCondition);
        ExpressionBuilder = source.ExpressionBuilder;
    }

    public string? ActualExpression { get; }
    private readonly List<BaseAssertCondition> _assertions = new();
    IEnumerable<BaseAssertCondition> ISource.GetAssertions() => _assertions;
    BaseAssertCondition? ISource.GetLastAssertion() => _assertions.Count > 0 ? _assertions[^1] : null;
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
        _assertions.Add(assertCondition);
        return this;
    }

    private static async ValueTask<AssertionData> ConvertAsync(IValueSource<TFromType> valueSource, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition)
    {
        var invokableAssertionBuilder = valueSource.RegisterAssertion(convertToAssertCondition, [], null);

        var assertionData = await invokableAssertionBuilder.GetAssertionData();
        var modifiedData = assertionData with { Result = convertToAssertCondition.ConvertedValue, End = DateTimeOffset.Now };
        await invokableAssertionBuilder.ProcessAssertionsAsync(modifiedData);
        return modifiedData;

    }
}
