using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

public class PairwiseDataSourceTests
{
    [Test]
    [Arguments("2,2,4,4", 16)]
    [Arguments("2,2,3,3,2", 10)]
    [Arguments("2,2,2,2,2", 6)]
    [Arguments("3,3,3,3", 9)]
    [Arguments("3,4,5,6", 30)]
    [Arguments("10,10,10", 106)]
    [Arguments("2,2,2,2,2,2,2,2,2,2", 9)]
    [Arguments("3,3,5,2,3", 17)]
    public async Task Covers_All_Pairs_With_Expected_Case_Count(string shape, int expectedCount)
    {
        var valueSets = CreateValueSets(shape);

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, [], PairwiseDataSourceAttribute.DefaultSeed);

        await Assert.That(rows.Count).IsEqualTo(expectedCount);
        await Assert.That(GetUncoveredPairs(valueSets, rows, [])).IsEmpty();
    }

    [Test]
    [Arguments("2,2,4,4")]
    [Arguments("3,4,5,6")]
    [Arguments("10,10,10")]
    public async Task Is_Deterministic(string shape)
    {
        var valueSets = CreateValueSets(shape);

        var first = Format(PairwiseDataSourceAttribute.GetTestCases(valueSets, [], PairwiseDataSourceAttribute.DefaultSeed));
        var second = Format(PairwiseDataSourceAttribute.GetTestCases(valueSets, [], PairwiseDataSourceAttribute.DefaultSeed));

        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    [Arguments(1)]
    [Arguments(42)]
    [Arguments(int.MaxValue)]
    [Arguments(-7)]
    public async Task Other_Seeds_Still_Cover_All_Pairs(int seed)
    {
        var valueSets = CreateValueSets("3,4,5,6");

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, [], seed);

        await Assert.That(GetUncoveredPairs(valueSets, rows, [])).IsEmpty();
        await Assert.That(rows.Count).IsLessThan(3 * 4 * 5 * 6);
    }

    [Test]
    public async Task Different_Seeds_Can_Produce_Different_Cases()
    {
        var valueSets = CreateValueSets("3,4,5,6");

        var first = Format(PairwiseDataSourceAttribute.GetTestCases(valueSets, [], PairwiseDataSourceAttribute.DefaultSeed));
        var second = Format(PairwiseDataSourceAttribute.GetTestCases(valueSets, [], 42));

        await Assert.That(second).IsNotEqualTo(first);
    }

    [Test]
    public async Task Exclusions_Are_Never_Generated_And_Reachable_Pairs_Stay_Covered()
    {
        var valueSets = CreateValueSets("3,4,5,6");
        object?[][] exclusions =
        [
            [0, 0, 0, 0],
            [1, 1, 1, 1],
            [2, 3, 4, 5],
            [0, 1, 2, 3],
        ];

        // Excluding every case with (p0 = 0, p1 = 0) makes that pair unreachable.
        var allExclusions = new List<object?[]>(exclusions);
        for (var c = 0; c < 5; c++)
        {
            for (var d = 0; d < 6; d++)
            {
                allExclusions.Add([0, 0, c, d]);
            }
        }

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, [.. allExclusions], PairwiseDataSourceAttribute.DefaultSeed);

        foreach (var row in rows)
        {
            await Assert.That(MatrixParameterValues.IsExcluded([.. allExclusions], row)).IsFalse();
        }

        await Assert.That(GetUncoveredPairs(valueSets, rows, [.. allExclusions])).IsEmpty();
    }

    [Test]
    public async Task Unreachable_Pair_With_Many_Parameters_Still_Covers_Reachable_Pairs()
    {
        // Five parameters of size 4 => 1024 Cartesian rows. Excluding every row that pairs
        // p0=0 with p1=0 makes that pair impossible; generation must drop both mirrored
        // tuple orientations without timing out, and still cover every reachable pair.
        IReadOnlyList<object?>[] valueSets =
        [
            [0, 1, 2, 3],
            [0, 1, 2, 3],
            [0, 1, 2, 3],
            [0, 1, 2, 3],
            [0, 1, 2, 3],
        ];

        var exclusions = new List<object?[]>();
        for (var c = 0; c < 4; c++)
        for (var d = 0; d < 4; d++)
        for (var e = 0; e < 4; e++)
        {
            exclusions.Add([0, 0, c, d, e]);
        }

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, [.. exclusions], PairwiseDataSourceAttribute.DefaultSeed);

        await Assert.That(rows.Count).IsGreaterThan(0);
        foreach (var row in rows)
        {
            await Assert.That(MatrixParameterValues.IsExcluded([.. exclusions], row)).IsFalse();
        }

        await Assert.That(GetUncoveredPairs(valueSets, rows, [.. exclusions])).IsEmpty();
    }

    [Test]
    public async Task Enum_Exclusions_Match_Underlying_Values()
    {
        IReadOnlyList<object?>[] valueSets =
        [
            [0, 1, 2],
            [true, false],
            ["a", "b", "c"],
        ];

        object?[][] exclusions =
        [
            [DayOfWeek.Monday, true, "a"],
        ];

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, exclusions, PairwiseDataSourceAttribute.DefaultSeed);

        foreach (var row in rows)
        {
            var isExcludedRow = Equals(row[0], 1) && Equals(row[1], true) && Equals(row[2], "a");
            await Assert.That(isExcludedRow).IsFalse();
        }

        await Assert.That(GetUncoveredPairs(valueSets, rows, exclusions)).IsEmpty();
    }

    [Test]
    public async Task Single_Parameter_Yields_Each_Value_Once()
    {
        IReadOnlyList<object?>[] valueSets = [[1, 2, 3, 4]];

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, [], PairwiseDataSourceAttribute.DefaultSeed);

        var values = new List<int>();
        foreach (var row in rows)
        {
            values.Add((int)row[0]!);
        }

        values.Sort();
        await Assert.That(values).IsEquivalentTo([1, 2, 3, 4]);
        await Assert.That(rows.Count).IsEqualTo(4);
    }

    [Test]
    public async Task Two_Parameters_Yield_Full_Product()
    {
        var valueSets = CreateValueSets("3,4");

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, [], PairwiseDataSourceAttribute.DefaultSeed);

        await Assert.That(rows.Count).IsEqualTo(12);
        await Assert.That(GetUncoveredPairs(valueSets, rows, [])).IsEmpty();
    }

    [Test]
    public async Task Empty_Value_Set_Yields_No_Cases()
    {
        IReadOnlyList<object?>[] valueSets = [[1, 2], [], [true, false]];

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, [], PairwiseDataSourceAttribute.DefaultSeed);

        await Assert.That(rows).IsEmpty();
    }

    [Test]
    public async Task All_Cases_Excluded_Yields_No_Cases()
    {
        IReadOnlyList<object?>[] valueSets = [[1], [true]];

        var rows = PairwiseDataSourceAttribute.GetTestCases(valueSets, [[1, true]], PairwiseDataSourceAttribute.DefaultSeed);

        await Assert.That(rows).IsEmpty();
    }

    private static IReadOnlyList<object?>[] CreateValueSets(string shape)
    {
        var sizes = shape.Split(',');
        var valueSets = new IReadOnlyList<object?>[sizes.Length];
        for (var i = 0; i < sizes.Length; i++)
        {
            var values = new object?[int.Parse(sizes[i])];
            for (var v = 0; v < values.Length; v++)
            {
                values[v] = v;
            }

            valueSets[i] = values;
        }

        return valueSets;
    }

    private static string Format(List<object?[]> rows)
    {
        var lines = new List<string>(rows.Count);
        foreach (var row in rows)
        {
            lines.Add(string.Join(",", row));
        }

        return string.Join("|", lines);
    }

    /// <summary>
    /// Returns every pair of values that appears in at least one allowed combination
    /// of the full Cartesian product but in none of <paramref name="rows"/>.
    /// </summary>
    private static List<string> GetUncoveredPairs(IReadOnlyList<object?>[] valueSets, List<object?[]> rows, object?[][] exclusions)
    {
        var reachable = new HashSet<string>();
        foreach (var combination in CartesianProductHelper.GetCartesianProduct(valueSets))
        {
            if (!MatrixParameterValues.IsExcluded(exclusions, combination))
            {
                AddPairs(reachable, combination);
            }
        }

        var covered = new HashSet<string>();
        foreach (var row in rows)
        {
            AddPairs(covered, row);
        }

        reachable.ExceptWith(covered);
        return [.. reachable];
    }

    private static void AddPairs(HashSet<string> pairs, object?[] row)
    {
        for (var i = 0; i < row.Length; i++)
        {
            for (var j = i + 1; j < row.Length; j++)
            {
                pairs.Add($"{i}={row[i]};{j}={row[j]}");
            }
        }
    }
}
