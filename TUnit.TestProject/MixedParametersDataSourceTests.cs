using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class MixedParametersDataSourceTests
{
    public static IEnumerable<string> GetStrings()
    {
        yield return "Hello";
        yield return "World";
    }

    public static IEnumerable<int> GetNumbers()
    {
        yield return 10;
        yield return 20;
        yield return 30;
    }

    public static IEnumerable<bool> GetBools()
    {
        yield return true;
        yield return false;
    }

    #region Basic Tests - Arguments Only

    [Test]
    [MixedParametersDataSource]
    public async Task TwoParameters_Arguments(
        [Arguments(1, 2, 3)] int x,
        [Arguments("a", "b")] string y)
    {
        // Should create 3 × 2 = 6 test cases
        await Assert.That(x).IsIn([1, 2, 3]);
        await Assert.That(y).IsIn(["a", "b"]);
    }

    [Test]
    [MixedParametersDataSource]
    public async Task ThreeParameters_Arguments(
        [Arguments(1, 2)] int x,
        [Arguments("a", "b", "c")] string y,
        [Arguments(true, false)] bool z)
    {
        // Should create 2 × 3 × 2 = 12 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn(["a", "b", "c"]);
        await Assert.That(z).IsIn([true, false]);
    }

    [Test]
    [MixedParametersDataSource]
    public async Task FourParameters_Arguments(
        [Arguments(1, 2)] int w,
        [Arguments("a", "b")] string x,
        [Arguments(true, false)] bool y,
        [Arguments(0.5, 1.5)] double z)
    {
        // Should create 2 × 2 × 2 × 2 = 16 test cases
        await Assert.That(w).IsIn([1, 2]);
        await Assert.That(x).IsIn(["a", "b"]);
        await Assert.That(y).IsIn([true, false]);
        await Assert.That(z).IsIn([0.5, 1.5]);
    }

    #endregion

    #region Mixing Arguments with MethodDataSource

    [Test]
    [MixedParametersDataSource]
    public async Task ArgumentsWithMethodDataSource(
        [Arguments(1, 2)] int x,
        [MethodDataSource(nameof(GetStrings))] string y)
    {
        // Should create 2 × 2 = 4 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn(["Hello", "World"]);
    }

    [Test]
    [MixedParametersDataSource]
    public async Task MultipleMethodDataSources(
        [MethodDataSource(nameof(GetNumbers))] int x,
        [MethodDataSource(nameof(GetStrings))] string y)
    {
        // Should create 3 × 2 = 6 test cases
        await Assert.That(x).IsIn([10, 20, 30]);
        await Assert.That(y).IsIn(["Hello", "World"]);
    }

    [Test]
    [MixedParametersDataSource]
    public async Task ThreeWayMix_ArgumentsAndMethodDataSources(
        [Arguments(1, 2)] int x,
        [MethodDataSource(nameof(GetStrings))] string y,
        [MethodDataSource(nameof(GetBools))] bool z)
    {
        // Should create 2 × 2 × 2 = 8 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn(["Hello", "World"]);
        await Assert.That(z).IsIn([true, false]);
    }

    #endregion

    #region Multiple Attributes on Same Parameter

    [Test]
    [MixedParametersDataSource]
    public async Task MultipleArgumentsAttributesOnSameParameter(
        [Arguments(1, 2)]
        [Arguments(3, 4)] int x,
        [Arguments("a")] string y)
    {
        // Should create (2 + 2) × 1 = 4 test cases
        await Assert.That(x).IsIn([1, 2, 3, 4]);
        await Assert.That(y).IsEqualTo("a");
    }

    [Test]
    [MixedParametersDataSource]
    public async Task MixingMultipleDataSourcesPerParameter(
        [Arguments(1)]
        [MethodDataSource(nameof(GetNumbers))] int x,
        [Arguments("test")] string y)
    {
        // Should create (1 + 3) × 1 = 4 test cases
        await Assert.That(x).IsIn([1, 10, 20, 30]);
        await Assert.That(y).IsEqualTo("test");
    }

    #endregion

    #region Type Variety Tests

    [Test]
    [MixedParametersDataSource]
    public async Task DifferentPrimitiveTypes(
        [Arguments(1, 2)] int intVal,
        [Arguments("a", "b")] string stringVal,
        [Arguments(1.5, 2.5)] double doubleVal,
        [Arguments(true, false)] bool boolVal,
        [Arguments('x', 'y')] char charVal)
    {
        // Should create 2 × 2 × 2 × 2 × 2 = 32 test cases
        await Assert.That(intVal).IsIn([1, 2]);
        await Assert.That(stringVal).IsIn(["a", "b"]);
        await Assert.That(doubleVal).IsIn([1.5, 2.5]);
        await Assert.That(boolVal).IsIn([true, false]);
        await Assert.That(charVal).IsIn(['x', 'y']);
    }

    [Test]
    [MixedParametersDataSource]
    public async Task NullableTypes(
        [Arguments(1, 2, null)] int? nullableInt,
        [Arguments("a", null)] string? nullableString)
    {
        // Should create 3 × 2 = 6 test cases
        if (nullableInt.HasValue)
        {
            await Assert.That(nullableInt.Value).IsIn([1, 2]);
        }
        // nullableString can be "a" or null
    }

    #endregion

    #region Edge Cases

    [Test]
    [MixedParametersDataSource]
    public async Task SingleParameterWithSingleValue(
        [Arguments(42)] int x)
    {
        // Should create 1 test case
        await Assert.That(x).IsEqualTo(42);
    }

    [Test]
    [MixedParametersDataSource]
    public async Task SingleParameterWithMultipleValues(
        [Arguments(1, 2, 3, 4, 5)] int x)
    {
        // Should create 5 test cases
        await Assert.That(x).IsIn([1, 2, 3, 4, 5]);
    }

    [Test]
    [MixedParametersDataSource]
    public async Task ManyParametersSmallSets(
        [Arguments(1)] int a,
        [Arguments(2)] int b,
        [Arguments(3)] int c,
        [Arguments(4)] int d,
        [Arguments(5)] int e)
    {
        // Should create 1 × 1 × 1 × 1 × 1 = 1 test case
        await Assert.That(a).IsEqualTo(1);
        await Assert.That(b).IsEqualTo(2);
        await Assert.That(c).IsEqualTo(3);
        await Assert.That(d).IsEqualTo(4);
        await Assert.That(e).IsEqualTo(5);
    }

    #endregion

    #region ClassDataSource Tests

    public class SimpleClass
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Test]
    [MixedParametersDataSource]
    public async Task WithClassDataSource(
        [Arguments(1, 2)] int x,
        [ClassDataSource<SimpleClass>] SimpleClass obj)
    {
        // Should create 2 × 1 = 2 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(obj).IsNotNull();
    }

    #endregion

    #region Generic Method Data Source Tests

    public static IEnumerable<T> GetGenericValues<T>(T first, T second)
    {
        yield return first;
        yield return second;
    }

    // Note: MethodDataSource with generic parameters and arguments needs special syntax
    // This test is simplified for now
    [Test]
    [MixedParametersDataSource]
    public async Task WithTypedMethodDataSource(
        [Arguments(1, 2)] int x,
        [MethodDataSource<MixedParametersDataSourceTests>(nameof(GetNumbers))] int y)
    {
        // Should create 2 × 3 = 6 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn([10, 20, 30]);
    }

    #endregion

    #region Verification Tests - Ensure Correct Combinations

    private static readonly HashSet<string> _seenCombinations = new();
    private static readonly object _lock = new();

    [Test]
    [MixedParametersDataSource]
    public async Task VerifyCartesianProduct_TwoParameters(
        [Arguments("A", "B")] string x,
        [Arguments(1, 2, 3)] int y)
    {
        // Should create 2 × 3 = 6 unique combinations
        var combination = $"{x}-{y}";

        bool isUnique;
        lock (_lock)
        {
            isUnique = _seenCombinations.Add(combination);
        }

        await Assert.That(isUnique).IsTrue();
        await Assert.That(x).IsIn(["A", "B"]);
        await Assert.That(y).IsIn([1, 2, 3]);
    }

    #endregion

    #region Performance Test - Many Combinations

    [Test]
    [MixedParametersDataSource]
    public async Task LargeCartesianProduct(
        [Arguments(1, 2, 3, 4, 5)] int a,
        [Arguments(1, 2, 3, 4)] int b,
        [Arguments(1, 2, 3)] int c)
    {
        // Should create 5 × 4 × 3 = 60 test cases
        await Assert.That(a).IsIn([1, 2, 3, 4, 5]);
        await Assert.That(b).IsIn([1, 2, 3, 4]);
        await Assert.That(c).IsIn([1, 2, 3]);
    }

    #endregion
}
