using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Extensions.Throws;

public static class ThrowsExtensions
{
    public static ThrowsException<TActual, TAnd, TOr> ThrowsException<TActual, TAnd, TOr>(this IThrows<TActual, TAnd, TOr> throws)
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return new(throws.Throws().AssertionBuilder, throws.Throws().ConnectorType, throws.Throws().OtherAssertCondition, exception => exception);
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> ThrowsNothing<TActual, TAnd, TOr>(this IThrows<TActual, TAnd, TOr> throws)
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(throws.Throws(), new ThrowsNothingAssertCondition<TActual, TAnd, TOr>(
            throws.Throws().AssertionBuilder.AppendCallerMethod(string.Empty)));
    }
}