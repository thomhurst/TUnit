using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;

namespace TUnit.Core;

/// <summary>
/// Enables combining different data sources on individual parameters to generate test cases through Cartesian product.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CombinedDataSourcesAttribute"/> allows you to apply different data source attributes
/// (such as <see cref="ArgumentsAttribute"/>, <see cref="MethodDataSourceAttribute"/>, <see cref="ClassDataSourceAttribute{T}"/>)
/// to individual parameters, creating test cases from all combinations via Cartesian product.
/// </para>
/// <para>
/// This is different from <see cref="MatrixDataSourceAttribute"/> which uses Matrix-specific attributes.
/// CombinedDataSource works with ANY <see cref="IDataSourceAttribute"/>, providing maximum flexibility
/// for complex data-driven testing scenarios.
/// </para>
/// <para>
/// <strong>Cartesian Product:</strong> If you have 3 parameters with 2, 3, and 4 values respectively,
/// this will generate 2 × 3 × 4 = 24 test cases covering all possible combinations.
/// </para>
/// <para>
/// <strong>Requirements:</strong>
/// <list type="bullet">
/// <item>All parameters must have at least one <see cref="IDataSourceAttribute"/></item>
/// <item>Parameters can have multiple data source attributes (values are combined)</item>
/// <item>Works with both static and instance data sources</item>
/// <item>Supports AOT/Native compilation</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Basic Usage with Arguments:</strong></para>
/// <code>
/// [Test]
/// [CombinedDataSources]
/// public void BasicTest(
///     [Arguments(1, 2, 3)] int x,
///     [Arguments("a", "b")] string y)
/// {
///     // Creates 3 × 2 = 6 test cases:
///     // (1,"a"), (1,"b"), (2,"a"), (2,"b"), (3,"a"), (3,"b")
/// }
/// </code>
///
/// <para><strong>Mixing Different Data Sources:</strong></para>
/// <code>
/// public static IEnumerable&lt;string&gt; GetStrings()
/// {
///     yield return "Hello";
///     yield return "World";
/// }
///
/// [Test]
/// [CombinedDataSources]
/// public void MixedTest(
///     [Arguments(1, 2)] int x,
///     [MethodDataSource(nameof(GetStrings))] string y,
///     [ClassDataSource&lt;MyClass&gt;] MyClass obj)
/// {
///     // Creates 2 × 2 × 1 = 4 test cases
///     // Combines Arguments, MethodDataSource, and ClassDataSource
/// }
/// </code>
///
/// <para><strong>Multiple Data Sources on Same Parameter:</strong></para>
/// <code>
/// [Test]
/// [CombinedDataSources]
/// public void MultipleSourcesTest(
///     [Arguments(1, 2)]
///     [Arguments(3, 4)] int x,
///     [Arguments("test")] string y)
/// {
///     // Creates (2 + 2) × 1 = 4 test cases
///     // x can be: 1, 2, 3, or 4
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CombinedDataSourcesAttribute : AsyncUntypedDataSourceGeneratorAttribute, IAccessesInstanceData
{
    protected override async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var parameterInformation = dataGeneratorMetadata
            .MembersToGenerate
            .OfType<ParameterMetadata>()
            .ToArray();

        if (parameterInformation.Length != dataGeneratorMetadata.MembersToGenerate.Length
            || parameterInformation.Length is 0)
        {
            throw new Exception("[CombinedDataSources] only supports parameterised tests");
        }

        if (dataGeneratorMetadata.TestInformation == null)
        {
            throw new InvalidOperationException("CombinedDataSource requires test information but none is available. This may occur during static property initialization.");
        }

        // For each parameter, collect all possible values (individual values, not arrays)
        var parameterValueSets = new List<IReadOnlyList<object?>>();

        foreach (var param in parameterInformation)
        {
            var parameterValues = await GetParameterValues(param, dataGeneratorMetadata);
            parameterValueSets.Add(parameterValues);
        }

        // Compute Cartesian product of all parameter value sets
        foreach (var combination in GetCartesianProduct(parameterValueSets))
        {
            yield return () => Task.FromResult(combination.ToArray())!;
        }
    }

    private async Task<IReadOnlyList<object?>> GetParameterValues(ParameterMetadata parameterMetadata, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Get all IDataSourceAttribute attributes on this parameter
        // Prefer cached attributes from source generator for AOT compatibility
        IDataSourceAttribute[] dataSourceAttributes;

        if (parameterMetadata.CachedDataSourceAttributes != null)
        {
            // Source-generated mode: use cached attributes (no reflection!)
            dataSourceAttributes = parameterMetadata.CachedDataSourceAttributes;
        }
        else
        {
            // Reflection mode: fall back to runtime attribute discovery
            if (parameterMetadata.ReflectionInfo == null)
            {
                throw new InvalidOperationException($"Parameter reflection information is not available for parameter '{parameterMetadata.Name}'. This typically occurs when using instance method data sources which are not supported at compile time.");
            }

            dataSourceAttributes = parameterMetadata.ReflectionInfo
                .GetCustomAttributesSafe()
                .OfType<IDataSourceAttribute>()
                .ToArray();
        }

        if (dataSourceAttributes.Length == 0)
        {
            throw new InvalidOperationException($"Parameter '{parameterMetadata.Name}' has no data source attributes. All parameters must have at least one IDataSourceAttribute when using [CombinedDataSources].");
        }

        var allValues = new List<object?>();

        // Process each data source attribute
        foreach (var dataSourceAttr in dataSourceAttributes)
        {
            // Check if this is an instance data attribute and we don't have an instance
            if (dataSourceAttr is IAccessesInstanceData && dataGeneratorMetadata.TestClassInstance == null)
            {
                var className = dataGeneratorMetadata.TestInformation?.Class.Type.Name ?? "Unknown";
                throw new InvalidOperationException(
                    $"Cannot use instance-based data source attribute on parameter '{parameterMetadata.Name}' when no instance is available. " +
                    $"Consider using static data sources or ensure the test class is properly instantiated.");
            }

            // Create metadata for this single parameter
            var singleParamMetadata = new DataGeneratorMetadata
            {
                TestBuilderContext = dataGeneratorMetadata.TestBuilderContext,
                MembersToGenerate = [parameterMetadata],
                TestInformation = dataGeneratorMetadata.TestInformation,
                Type = dataGeneratorMetadata.Type,
                TestSessionId = dataGeneratorMetadata.TestSessionId,
                TestClassInstance = dataGeneratorMetadata.TestClassInstance,
                ClassInstanceArguments = dataGeneratorMetadata.ClassInstanceArguments
            };

            // Get data rows from this data source (need to await async enumerable)
            var dataRows = await ProcessDataSourceAsync(dataSourceAttr, singleParamMetadata);

            allValues.AddRange(dataRows);
        }

        if (allValues.Count == 0)
        {
            throw new InvalidOperationException($"Parameter '{parameterMetadata.Name}' data sources produced no values.");
        }

        return allValues;
    }

    private static async Task<List<object?>> ProcessDataSourceAsync(IDataSourceAttribute dataSourceAttr, DataGeneratorMetadata metadata)
    {
        var values = new List<object?>();

        // Special handling for ArgumentsAttribute when used on parameters with CombinedDataSource
        // ArgumentsAttribute yields ONE row containing ALL values, but for CombinedDataSource we need
        // each value to be treated as a separate option for this parameter
        if (dataSourceAttr is ArgumentsAttribute argsAttr)
        {
            // Each value in Arguments should be a separate option for this parameter
            values.AddRange(argsAttr.Values);
        }
        else
        {
            await foreach (var dataRowFunc in dataSourceAttr.GetDataRowsAsync(metadata))
            {
                var dataRow = await dataRowFunc();
                if (dataRow != null && dataRow.Length > 0)
                {
                    // Each data row should have exactly one element for this parameter
                    values.Add(dataRow[0]);
                }
            }
        }

        return values;
    }

    private readonly IEnumerable<IEnumerable<object?>> _seed = [[]];

    private IEnumerable<IEnumerable<object?>> GetCartesianProduct(IEnumerable<IReadOnlyList<object?>> parameterValueSets)
    {
        // Same algorithm as Matrix - compute Cartesian product
        return parameterValueSets.Aggregate(_seed, (accumulator, values)
            => accumulator.SelectMany(x => values.Select(x.Append)));
    }
}
