#nullable disable

using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static ExceptionMessage<TActual, TAnd, TOr> Message<TActual, TAnd, TOr>(this Has<TActual, TAnd, TOr> has) 
        where TActual : Exception
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return new ExceptionMessage<TActual, TAnd, TOr>(has.AssertionBuilder.AppendCallerMethod(null), has.ConnectorType, has.OtherAssertCondition);
    }
}