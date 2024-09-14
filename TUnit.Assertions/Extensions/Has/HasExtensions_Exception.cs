#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static ExceptionMessage<TActual, TAnd, TOr> HasMessage<TActual, TAnd, TOr>(this IHas<TActual, TAnd, TOr> has) 
        where TActual : Exception
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new ExceptionMessage<TActual, TAnd, TOr>(has.Has().AssertionBuilder.AppendCallerMethod(null), has.Has().ConnectorType, has.Has().OtherAssertCondition);
    }
}