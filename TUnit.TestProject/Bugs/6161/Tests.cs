using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6161;

// Issue #6161: a [MethodDataSource] returning IEnumerable<TestDataRow<Func<...>>> must invoke the
// inner Func and spread the resulting tuple into the test method parameters - matching the behaviour
// of a bare IEnumerable<Func<...>> data source. Before the fix the Func was passed through as a
// single argument, so the tuple never bound to (int a, int b, int c) and the parameterized
// DisplayName placeholders could not be substituted (causing rows with the same template to collapse).
[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    private static readonly ConcurrentBag<(int, int, int)> ExecutedTuples = [];

    [Test]
    [MethodDataSource(nameof(TupleData))]
    public async Task TupleFuncIsUnwrapped(int a, int b, int c)
    {
        ExecutedTuples.Add((a, b, c));
        await Assert.That(a + b).IsEqualTo(c);
    }

    [Test]
    [MethodDataSource(nameof(RecordData))]
    public async Task ReferenceFuncIsUnwrapped(Calculation calculation)
    {
        await Assert.That(calculation.First + calculation.Second).IsEqualTo(calculation.Expected);
    }

    // Two rows deliberately share the same DisplayName template. Once the Func is invoked the
    // placeholders resolve to distinct values, so both rows run as separate test cases.
    public static IEnumerable<TestDataRow<Func<(int, int, int)>>> TupleData()
    {
        yield return new(static () => (1, 1, 2), DisplayName: "$arg1 + $arg2 = $arg3");
        yield return new(static () => (1, 2, 3), DisplayName: "$arg1 + $arg2 = $arg3");
        yield return new(static () => (2, 3, 5), DisplayName: "$arg1 + $arg2 = $arg3");
    }

    public static IEnumerable<TestDataRow<Func<Calculation>>> RecordData()
    {
        yield return new(static () => new Calculation(1, 1, 2), DisplayName: "Calc one");
        yield return new(static () => new Calculation(4, 5, 9), DisplayName: "Calc two");
    }

    // Regression for the dedup symptom: if rows had collapsed, fewer than the three distinct tuples
    // would have executed.
    [After(Class)]
    public static async Task AllDistinctRowsExecuted()
    {
        await Assert.That(ExecutedTuples.Distinct().Count()).IsEqualTo(3);
    }

    public record Calculation(int First, int Second, int Expected);
}
