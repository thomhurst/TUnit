#nullable disable

using System.Collections;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public static class HasExtensions
{
    public static ExceptionMessage<TActual, TAnd, TOr> Message<TActual, TAnd, TOr>(this Has<TActual, TAnd, TOr> has) 
        where TActual : Exception
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return new ExceptionMessage<TActual, TAnd, TOr>(has.AssertionBuilder.AppendCallerMethod(null), has.ConnectorType, has.OtherAssertCondition);
    }
    
    public static EnumerableCount<TActual, TAnd, TOr> Count<TActual, TAnd, TOr>(this Has<TActual, TAnd, TOr> has) 
        where TActual : IEnumerable
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return new EnumerableCount<TActual, TAnd, TOr>(has.AssertionBuilder.AppendCallerMethod(null), has.ConnectorType, has.OtherAssertCondition);
    }
    
    public static StringLength<TAnd, TOr> Length<TAnd, TOr>(this Has<string, TAnd, TOr> has)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return new StringLength<TAnd, TOr>(has.AssertionBuilder.AppendCallerMethod(null), has.ConnectorType, has.OtherAssertCondition);
    }
}