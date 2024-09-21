namespace TUnit.Assertions.AssertionBuilders;

public class AndAssertionBuilder<TActual> : AssertionBuilder<TActual>, IAndAssertionBuilder
{
    internal AndAssertionBuilder(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
    }
}