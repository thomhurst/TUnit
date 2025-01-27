using System.Text;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface ISource
{
    string? ActualExpression { get; }
    internal Stack<BaseAssertCondition> Assertions { get; }
    internal ValueTask<AssertionData> AssertionDataTask { get; }
    internal StringBuilder ExpressionBuilder { get; }

    ISource AppendExpression(string expression);
    ISource WithAssertion(BaseAssertCondition assertCondition);
}