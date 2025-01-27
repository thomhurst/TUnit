namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface ISource<out TActual>
{
    ISource<TActual> AppendExpression(string expression);
    ISource<TActual> WithAssertion(BaseAssertCondition assertCondition);
}