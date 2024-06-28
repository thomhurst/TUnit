using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.ClassMember;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions;

public class Has<TRootObject, TAnd, TOr> : Connector<TRootObject, TAnd, TOr>
    where TAnd : And<TRootObject, TAnd, TOr>, IAnd<TAnd, TRootObject, TAnd, TOr>
    where TOr : Or<TRootObject, TAnd, TOr>, IOr<TOr, TRootObject, TAnd, TOr>
{
    protected internal AssertionBuilder<TRootObject> AssertionBuilder { get; }

    public Has(AssertionBuilder<TRootObject> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TRootObject, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder
            .AppendConnector(connectorType)
            .AppendExpression("Has");
    }

    public Member<TRootObject, TPropertyType, TAnd, TOr> Member<TPropertyType>(Expression<Func<TRootObject, TPropertyType>> selector, [CallerArgumentExpression("selector")] string expression = "") => new(this, AssertionBuilder.AppendCallerMethod(expression), selector);
}