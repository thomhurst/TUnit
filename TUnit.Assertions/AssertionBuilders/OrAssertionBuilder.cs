namespace TUnit.Assertions.AssertionBuilders;

public class OrAssertionBuilder<TActual> : AssertionBuilder<TActual>, IOrAssertionBuilder
{
    internal OrAssertionBuilder(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
    }
}