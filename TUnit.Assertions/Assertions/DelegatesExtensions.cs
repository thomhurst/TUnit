using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Delegates;

namespace TUnit.Assertions;

public static class DelegatesExtensions
{
    public static Throws<TActual> Throws<TActual>(this IDelegateSource<TActual> delegateSource)
    {
        return new(delegateSource, exception => exception);
    }
}