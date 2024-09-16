#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static ExceptionMessage<TActual, TAnd, TOr> HasMessage<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TActual : Exception
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new ExceptionMessage<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null));
    }
}