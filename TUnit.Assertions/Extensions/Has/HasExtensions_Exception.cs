#nullable disable

using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static ExceptionMessage<TActual, TAnd, TOr> HasMessage<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TActual : Exception
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new ExceptionMessage<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null));
    }
}