using TUnit.Core.Helpers;

namespace TUnit.Core;

/// <summary>
/// Generates test cases from all combinations (Cartesian product) of parameter values.
/// </summary>
/// <remarks>
/// <para>
/// For boolean parameters, all values (<c>true</c>, <c>false</c>) are generated automatically.
/// For enum parameters, all defined enum values are generated automatically.
/// For other types, use <c>[Matrix(...)]</c> on individual parameters to specify the values.
/// </para>
/// <para>
/// Use <see cref="MatrixExclusionAttribute"/> on the test method to exclude specific combinations.
/// </para>
/// <para>
/// When the full Cartesian product is too large, consider <see cref="PairwiseDataSourceAttribute"/>,
/// which uses the same parameter attributes but only guarantees that every pair of values is covered.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Test, MatrixDataSource]
/// public void TestAllCombinations(
///     [Matrix(1, 2, 3)] int x,
///     [Matrix("a", "b")] string y)
/// {
///     // Generates 6 test cases: (1,"a"), (1,"b"), (2,"a"), (2,"b"), (3,"a"), (3,"b")
/// }
///
/// [Test, MatrixDataSource]
/// public void TestWithEnum(bool enabled, MyEnum mode)
/// {
///     // Automatically generates all bool x enum combinations
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MatrixDataSourceAttribute : UntypedDataSourceGeneratorAttribute, IAccessesInstanceData
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var parameterInformation = MatrixParameterValues.GetParameters(dataGeneratorMetadata, "MatrixDataSource");
        var exclusions = MatrixParameterValues.GetExclusions(dataGeneratorMetadata);
        var valueSets = MatrixParameterValues.GetValueSets(dataGeneratorMetadata, parameterInformation);

        foreach (var rowArray in CartesianProductHelper.GetCartesianProduct(valueSets))
        {
            if (MatrixParameterValues.IsExcluded(exclusions, rowArray))
            {
                continue;
            }

            yield return () => rowArray;
        }
    }
}
