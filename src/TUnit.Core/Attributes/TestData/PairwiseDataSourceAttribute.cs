using TUnit.Core.Helpers;

namespace TUnit.Core;

/// <summary>
/// Generates test cases that cover every <em>pair</em> of parameter values at least once
/// (all-pairs / pairwise testing), instead of every combination.
/// </summary>
/// <remarks>
/// <para>
/// This is an alternative to <see cref="MatrixDataSourceAttribute"/> for tests with many parameters,
/// where the full Cartesian product becomes too large. Most defects are triggered by the interaction of
/// at most two parameters, so covering every pair of values usually catches them with far fewer test cases.
/// For example, four parameters with 2, 2, 4 and 4 values produce 64 cases with
/// <see cref="MatrixDataSourceAttribute"/> but only 16 with <see cref="PairwiseDataSourceAttribute"/>.
/// </para>
/// <para>
/// Parameter values are discovered exactly as for <see cref="MatrixDataSourceAttribute"/>:
/// <c>[Matrix(...)]</c>, <c>[Matrix&lt;T&gt;]</c>, <c>[MatrixRange&lt;T&gt;]</c>, <c>[MatrixMethod&lt;T&gt;]</c>
/// and <c>[MatrixInstanceMethod&lt;T&gt;]</c> on individual parameters, with <see cref="MatrixAttribute.Excluding"/> honored.
/// <see langword="bool"/>, enum and nullable <see langword="bool"/>/enum parameters get all of their values automatically.
/// </para>
/// <para>
/// <see cref="MatrixExclusionAttribute"/> is applied as a constraint <em>during</em> generation,
/// so every pair of values that can appear in an allowed test case is still covered.
/// </para>
/// <para>
/// Generation is deterministic: the same parameter values and <see cref="Seed"/> always produce the same test cases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Test, PairwiseDataSource]
/// public void TestPairs(
///     bool largeArrayPool,
///     bool excessSpan,
///     [Matrix(0, 99, 100, 101)] int sizeHint,
///     [Matrix(1, 2, 3, 100)] int stepCount)
/// {
///     // 16 test cases instead of the 64 that [MatrixDataSource] would generate.
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PairwiseDataSourceAttribute : UntypedDataSourceGeneratorAttribute, IAccessesInstanceData
{
    /// <summary>
    /// The default value of <see cref="Seed"/>.
    /// </summary>
    public const int DefaultSeed = 15485863;

    /// <summary>
    /// Gets or sets the seed for the pseudo-random number generator used to build the covering set.
    /// The generated test cases are fully deterministic for a given seed and set of parameter values;
    /// changing the seed produces a different (but equally valid) set of test cases.
    /// </summary>
    public int Seed { get; set; } = DefaultSeed;

    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var parameterInformation = MatrixParameterValues.GetParameters(dataGeneratorMetadata, "PairwiseDataSource");
        var exclusions = MatrixParameterValues.GetExclusions(dataGeneratorMetadata);
        var valueSets = MatrixParameterValues.GetValueSets(dataGeneratorMetadata, parameterInformation);

        foreach (var row in GetTestCases(valueSets, exclusions, Seed))
        {
            yield return () => row;
        }
    }

    internal static List<object?[]> GetTestCases(IReadOnlyList<object?>[] valueSets, object?[][] exclusions, int seed)
    {
        var dimensions = new int[valueSets.Length];
        for (var i = 0; i < valueSets.Length; i++)
        {
            dimensions[i] = valueSets[i].Count;
            if (dimensions[i] == 0)
            {
                return [];
            }
        }

        Func<int[], bool>? isAllowed = null;
        if (exclusions.Length > 0)
        {
            // Shared across predicate invocations; generation is single-threaded, so this is safe.
            var buffer = new object?[valueSets.Length];
            isAllowed = indices =>
            {
                for (var i = 0; i < indices.Length; i++)
                {
                    buffer[i] = valueSets[i][indices[i]];
                }

                return !MatrixParameterValues.IsExcluded(exclusions, buffer);
            };
        }

        var testCases = PairwiseStrategy.GetTestCases(dimensions, isAllowed, seed);
        var rows = new List<object?[]>(testCases.Length);
        foreach (var testCase in testCases)
        {
            var row = new object?[testCase.Length];
            for (var i = 0; i < testCase.Length; i++)
            {
                row[i] = valueSets[i][testCase[i]];
            }

            rows.Add(row);
        }

        return rows;
    }
}
