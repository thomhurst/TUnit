using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Extensions.Throws;

public static class ThrowsExtensions
{
    public static ThrowsException<TActual, TAnd, TOr> ThrowsException<TActual, TAnd, TOr>(this IThrows<TActual, TAnd, TOr> throws)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new(throws.AssertionConnector.AssertionBuilder, throws.AssertionConnector.ChainType, throws.AssertionConnector.OtherAssertCondition, exception => exception);
    }
    
    public static BaseAssertCondition<TActual> ThrowsNothing<TActual, TAnd, TOr>(this IThrows<TActual, TAnd, TOr> throws)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(throws.AssertionConnector, new ThrowsNothingAssertCondition<TActual, TAnd, TOr>(
            throws.AssertionConnector.AssertionBuilder.AppendCallerMethod(string.Empty)));
    }
}