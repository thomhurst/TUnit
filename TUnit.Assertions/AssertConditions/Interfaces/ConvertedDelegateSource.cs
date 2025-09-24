using System.Text;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public class ConvertedDelegateSource<TToType> : IValueSource<TToType?> where TToType : Exception
{
    private readonly List<BaseAssertCondition> _assertions = new();

    public ConvertedDelegateSource(IDelegateSource source)
    {
        var convertToAssertCondition = new ConvertExceptionToValueAssertCondition<TToType>();

        ActualExpression = source.ActualExpression;

        var lastAssertion = source.GetLastAssertion();
        if (lastAssertion is null)
        {
            throw new InvalidOperationException("No assertion found on source to convert");
        }

        _assertions.Add(new DelegateConversionAssertionCondition<TToType>(source, (BaseAssertCondition<object?>) lastAssertion));
        AssertionDataTask = ConvertAsync(source, convertToAssertCondition);
        ExpressionBuilder = source.ExpressionBuilder;
    }

    public string? ActualExpression { get; }
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

    private static async ValueTask<AssertionData> ConvertAsync(IDelegateSource delegateSource, ConvertExceptionToValueAssertCondition<TToType> convertToAssertCondition)
    {
        var invokableAssertionBuilder = delegateSource.RegisterAssertion(convertToAssertCondition, [], null);

        var assertionData = await invokableAssertionBuilder.GetAssertionData();
        var modifiedData = assertionData with { Result = convertToAssertCondition.ConvertedExceptionValue, Exception = null, End = DateTimeOffset.Now };
        await invokableAssertionBuilder.ProcessAssertionsAsync(modifiedData);
        return modifiedData;

    }
}
