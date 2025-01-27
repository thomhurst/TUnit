namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface ISource
{
    string? ActualExpression { get; }
    ISource AppendExpression(string expression);
    ISource WithAssertion(BaseAssertCondition assertCondition);
}