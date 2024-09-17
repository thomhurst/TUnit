using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class OrAssertionBuilder<TActual, TAnd, TOr> : AssertionBuilder<TActual, TAnd, TOr>, IOrAssertionBuilder 
    where TOr : IOr<TActual, TAnd, TOr> where TAnd : IAnd<TActual, TAnd, TOr>
{
    public OrAssertionBuilder(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
    }
}