using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class AndAssertionBuilder<TActual, TAnd, TOr> : AssertionBuilder<TActual, TAnd, TOr>, IAndAssertionBuilder 
    where TOr : IOr<TActual, TAnd, TOr> where TAnd : IAnd<TActual, TAnd, TOr>
{
    internal AndAssertionBuilder(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) : base(assertionBuilder.AssertionDataDelegate, assertionBuilder.ActualExpression!, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
    }
}