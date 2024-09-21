#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static ExceptionMessage<TActual> HasMessage<TActual>(this IDelegateSource<TActual> delegateSource) 
        where TActual : Exception
    {
        delegateSource.AssertionBuilder.AppendCallerMethod([]);
        return new ExceptionMessage<TActual>(delegateSource);
    }
}