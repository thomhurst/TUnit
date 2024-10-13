#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static ExceptionMessage<TActual> HasMessage<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : Exception
    {
        valueSource.AssertionBuilder.AppendCallerMethod([]);
        return new ExceptionMessage<TActual>(valueSource);
    }
}