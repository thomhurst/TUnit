using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class AndAssertionBuilder<TActual, TAnd, TOr> : AssertionBuilder<TActual, TAnd, TOr>, IAndAssertionBuilder 
    where TOr : IOr<TActual, TAnd, TOr> where TAnd : IAnd<TActual, TAnd, TOr>
{
    public AndAssertionBuilder(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.RawActualExpression!, assertionBuilder.AssertionMessage, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
    }
}