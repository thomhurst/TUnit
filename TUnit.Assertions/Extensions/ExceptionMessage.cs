using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public class ExceptionMessage<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr> 
    where TActual : Exception
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public ExceptionMessage(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Message");;
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(string expected)
    {
        return EqualTo(expected, StringComparison.Ordinal);
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(string expected, StringComparison stringComparison)
    {
        return Wrap(new DelegateAssertCondition<TActual, string, TAnd, TOr>(AssertionBuilder, expected, (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return string.Equals(actual.Message, expected, stringComparison);
            },
            (_, actual) =>
                $"Exception had a message of '{actual?.Message}' instead of '{expected}'")
        );
    }
}