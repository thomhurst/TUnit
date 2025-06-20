#nullable disable

using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class GenericIsInExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsIn<TActual>(this IValueSource<TActual> valueSource, params TActual[] expected)
    {
        return IsIn(valueSource, expected, EqualityComparer<TActual>.Default);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsIn<TActual>(this IValueSource<TActual> valueSource, IEnumerable<TActual> expected)
    {
        return IsIn(valueSource, expected, EqualityComparer<TActual>.Default);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsIn<TActual>(this IValueSource<TActual> valueSource, IEnumerable<TActual> expected, IEqualityComparer<TActual> equalityComparer)
    {
        var expectedArray = expected as TActual[] ?? expected.ToArray();
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual[]>(
            expectedArray,
            (actual, expectedValues, _) =>
            {
                if (actual == null && expectedValues.Any(e => e == null))
                {
                    return true;
                }
                if (actual == null)
                {
                    return false;
                }
                return expectedValues.Contains(actual, equalityComparer);
            },
            (actual, expectedValues, _) => $"{actual} was not found in the expected values: [{string.Join(", ", expectedValues)}]",
            $"be in [{string.Join(", ", expectedArray)}]"
        ), [$"[{string.Join(", ", expectedArray)}]"]);
    }
}