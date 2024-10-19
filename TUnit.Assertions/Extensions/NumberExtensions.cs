using System.Numerics;

namespace TUnit.Assertions.Extensions;

internal static class NumberExtensions
{
    public static bool IsBetween<TActual>(this INumber<TActual> number, INumber<TActual> min, INumber<TActual> max) 
        where TActual : INumber<TActual>
    {
        return number.CompareTo(min) >= 0 && number.CompareTo(max) <= 0;
    }
}