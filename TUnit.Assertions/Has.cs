using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public class Has<TActual, TAnd, TOr> : Connector<TActual?, TAnd, TOr>
    where TAnd : And<TActual?, TAnd, TOr>, IAnd<TAnd, TActual?, TAnd, TOr>
    where TOr : Or<TActual?, TAnd, TOr>, IOr<TOr, TActual?, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual?> AssertionBuilder { get; }

    public Has(AssertionBuilder<TActual?> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual?, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public Property<TActual?, TAnd, TOr> Property(string name) => new(AssertionBuilder, name);

    public Property<TActual?, TPropertyType, TAnd, TOr> Property<TPropertyType>(string name) => new(AssertionBuilder, name);
}