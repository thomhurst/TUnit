using System.Collections.Generic;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NestedTupleDataSourceTests
{
    // Test 1: Simple nested tuple - (int, (int, int)) unwrapped to int, (int, int)
    [Test]
    [MethodDataSource(nameof(NestedTupleData))]
    public async Task NestedTuple_SeparateParams(int value1, (int, int) value2)
    {
        await Assert.That(value1).IsGreaterThanOrEqualTo(1);
        await Assert.That(value2.Item1).IsGreaterThanOrEqualTo(2);
        await Assert.That(value2.Item2).IsGreaterThanOrEqualTo(3);
    }

    // Test 2: Single parameter receiving the full nested tuple
    [Test]
    [MethodDataSource(nameof(NestedTupleData))]
    public async Task NestedTuple_SingleParam((int, (int, int)) value)
    {
        await Assert.That(value.Item1).IsGreaterThanOrEqualTo(1);
        await Assert.That(value.Item2.Item1).IsGreaterThanOrEqualTo(2);
        await Assert.That(value.Item2.Item2).IsGreaterThanOrEqualTo(3);
    }

    // Test 3: Deeply nested tuple - ((int, int), (int, int)) unwrapped to two tuple parameters
    [Test]
    [MethodDataSource(nameof(DoublyNestedTupleData))]
    public async Task DoublyNestedTuple_SeparateParams((int, int) value1, (int, int) value2)
    {
        await Assert.That(value1.Item1).IsGreaterThanOrEqualTo(1);
        await Assert.That(value1.Item2).IsGreaterThanOrEqualTo(2);
        await Assert.That(value2.Item1).IsGreaterThanOrEqualTo(3);
        await Assert.That(value2.Item2).IsGreaterThanOrEqualTo(4);
    }

    // Test 4: Triple nested - (int, (int, (int, int))) unwrapped to int, (int, (int, int))
    [Test]
    [MethodDataSource(nameof(TripleNestedTupleData))]
    public async Task TripleNestedTuple_SeparateParams(int value1, (int, (int, int)) value2)
    {
        await Assert.That(value1).IsEqualTo(1);
        await Assert.That(value2.Item1).IsEqualTo(2);
        await Assert.That(value2.Item2.Item1).IsEqualTo(3);
        await Assert.That(value2.Item2.Item2).IsEqualTo(4);
    }

    // Test 5: Mixed types in nested tuple - (string, (int, bool))
    [Test]
    [MethodDataSource(nameof(MixedNestedTupleData))]
    public async Task MixedNestedTuple_SeparateParams(string value1, (int, bool) value2)
    {
        await Assert.That(value1).IsEqualTo("test");
        await Assert.That(value2.Item1).IsEqualTo(42);
        await Assert.That(value2.Item2).IsTrue();
    }

    // Test 6: Nested tuple with array - (int[], (string, double))
    [Test]
    [MethodDataSource(nameof(ArrayNestedTupleData))]
    public async Task ArrayNestedTuple_SeparateParams(int[] value1, (string, double) value2)
    {
        await Assert.That(value1).HasCount(3);
        await Assert.That(value2.Item1).IsEqualTo("array");
        await Assert.That(value2.Item2).IsEqualTo(3.14);
    }

    // Test 7: IEnumerable with nested tuples
    [Test]
    [MethodDataSource(nameof(EnumerableNestedTupleData))]
    public async Task EnumerableNestedTuple_SeparateParams(int value1, (int, int) value2)
    {
        await Assert.That(value1).IsGreaterThan(0);
        await Assert.That(value2.Item1).IsGreaterThan(0);
        await Assert.That(value2.Item2).IsGreaterThan(0);
    }

    // Test 8: Async enumerable with nested tuples
    [Test]
    [MethodDataSource(nameof(AsyncEnumerableNestedTupleData))]
    public async Task AsyncEnumerableNestedTuple_SeparateParams(int value1, (int, int) value2)
    {
        await Assert.That(value1).IsGreaterThan(0);
        await Assert.That(value2.Item1).IsGreaterThan(0);
        await Assert.That(value2.Item2).IsGreaterThan(0);
    }

    // Data source methods
    public static IEnumerable<Func<(int, (int, int))>> NestedTupleData()
    {
        return
        [
            () => (1, (2, 3)),
            () => (4, (5, 6)),
            () => (7, (8, 9))
        ];
    }

    public static IEnumerable<Func<((int, int), (int, int))>> DoublyNestedTupleData()
    {
        return
        [
            () => ((1, 2), (3, 4)),
            () => ((5, 6), (7, 8))
        ];
    }

    public static Func<(int, (int, (int, int)))> TripleNestedTupleData()
    {
        return () => (1, (2, (3, 4)));
    }

    public static IEnumerable<(string, (int, bool))> MixedNestedTupleData()
    {
        yield return ("test", (42, true));
    }

    public static (int[], (string, double)) ArrayNestedTupleData()
    {
        return ([1, 2, 3], ("array", 3.14));
    }

    public static IEnumerable<(int, (int, int))> EnumerableNestedTupleData()
    {
        for (var i = 1; i <= 3; i++)
        {
            yield return (i, (i + 1, i + 2));
        }
    }

    public static async IAsyncEnumerable<(int, (int, int))> AsyncEnumerableNestedTupleData()
    {
        for (var i = 1; i <= 3; i++)
        {
            await Task.Yield();
            yield return (i, (i + 10, i + 20));
        }
    }
}
