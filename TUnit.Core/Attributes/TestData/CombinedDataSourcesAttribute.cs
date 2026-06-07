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
/// CombinedDataSources works with ANY <see cref="IDataSourceAttribute"/>, providing maximum flexibility
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
public sealed class CombinedDataSourcesAttribute : AsyncUntypedDataSourceGeneratorAttribute
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
            throw new InvalidOperationException("CombinedDataSources requires test information but none is available. This may occur during static property initialization.");
        }

        // For each parameter, collect a factory per possible value (individual values, not arrays).
        // Values are materialized per combination (not once up front) so that non-shared
        // reference values - e.g. [ClassDataSource] with SharedType.None - produce a fresh
        // instance for every test case instead of one instance shared across the cartesian
        // product, which races during parallel property injection/initialization (#6177).
        var parameterValueFactorySets = new List<IReadOnlyList<Func<Task<object?>>>>();

        foreach (var param in parameterInformation)
        {
            var parameterValueFactories = await GetParameterValueFactories(param, dataGeneratorMetadata);
            parameterValueFactorySets.Add(parameterValueFactories);
        }

        // Compute Cartesian product of all parameter value factory sets
        foreach (var combination in GetCartesianProduct(parameterValueFactorySets))
        {
            yield return async () =>
            {
                var row = new object?[combination.Length];
                for (var i = 0; i < combination.Length; i++)
                {
                    row[i] = await combination[i]();
                }

                return row;
            };
        }
    }

    private async Task<IReadOnlyList<Func<Task<object?>>>> GetParameterValueFactories(ParameterMetadata parameterMetadata, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Get all IDataSourceAttribute attributes on this parameter
        // Prefer cached attributes from source generator for AOT compatibility
        IDataSourceAttribute[] dataSourceAttributes;

        if (parameterMetadata.CachedDataSourceAttributes != null)
        {
            // Source-generated mode: use cached attributes (no reflection!)
            dataSourceAttributes = parameterMetadata.CachedDataSourceAttributes
                .OfType<IDataSourceAttribute>()
                .ToArray();
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

        var allValueFactories = new List<Func<Task<object?>>>();

        // Process each data source attribute
        foreach (var dataSourceAttr in dataSourceAttributes)
        {
            // Check if this is an instance data attribute and we don't have an instance
            if (dataSourceAttr is IAccessesInstanceData && dataGeneratorMetadata.TestClassInstance == null)
            {
                var className = dataGeneratorMetadata.TestInformation?.Class.Type.Name ?? "Unknown";
                var attrName = dataSourceAttr.GetType().Name;
                throw new InvalidOperationException(
                    $"Cannot use instance-based data source '{attrName}' on parameter '{parameterMetadata.Name}' in class '{className}'. " +
                    $"When [CombinedDataSources] is applied at the class level (constructor parameters), all data sources must be static " +
                    $"because no instance exists yet. Use static [MethodDataSource] or [Arguments] instead, " +
                    $"or move [CombinedDataSources] to the method level if you need instance-based data sources.");
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
                ClassInstanceArguments = dataGeneratorMetadata.ClassInstanceArguments,
                InstanceFactory = dataGeneratorMetadata.InstanceFactory
            };

            // Get data row factories from this data source (need to await async enumerable)
            var dataRowFactories = await ProcessDataSourceAsync(dataSourceAttr, singleParamMetadata);

            allValueFactories.AddRange(dataRowFactories);
        }

        if (allValueFactories.Count == 0)
        {
            throw new InvalidOperationException($"Parameter '{parameterMetadata.Name}' data sources produced no values.");
        }

        return allValueFactories;
    }

    private static async Task<List<Func<Task<object?>>>> ProcessDataSourceAsync(IDataSourceAttribute dataSourceAttr, DataGeneratorMetadata metadata)
    {
        var valueFactories = new List<Func<Task<object?>>>();

        // Special handling for ArgumentsAttribute when used on parameters with CombinedDataSources
        // ArgumentsAttribute yields ONE row containing ALL values, but for CombinedDataSources we need
        // each value to be treated as a separate option for this parameter
        if (dataSourceAttr is ArgumentsAttribute argsAttr)
        {
            // Each value in Arguments should be a separate option for this parameter
            foreach (var value in argsAttr.Values)
            {
                valueFactories.Add(() => Task.FromResult(value));
            }
        }
        else
        {
            await foreach (var dataRowFunc in dataSourceAttr.GetDataRowsAsync(metadata))
            {
                // Defer invocation: the row factory runs once per combination that uses it,
                // so each test case gets its own value (fresh instance for non-shared sources).
                valueFactories.Add(async () =>
                {
                    var dataRow = await dataRowFunc();

                    // Each data row should have exactly one element for this parameter
                    return dataRow is { Length: > 0 } ? dataRow[0] : null;
                });
            }
        }

        return valueFactories;
    }

    private static IEnumerable<T[]> GetCartesianProduct<T>(IReadOnlyList<IReadOnlyList<T>> parameterValueSets)
    {
        var dimensionCount = parameterValueSets.Count;

        // Any empty dimension makes the product empty (matches the previous
        // Aggregate/SelectMany behaviour where SelectMany over [] yields nothing).
        for (var dimension = 0; dimension < dimensionCount; dimension++)
        {
            if (parameterValueSets[dimension].Count == 0)
            {
                yield break;
            }
        }

        // Odometer-style Cartesian product: the last dimension varies fastest,
        // matching the previous Aggregate/SelectMany ordering exactly.
        var indices = new int[dimensionCount];

        while (true)
        {
            var row = new T[dimensionCount];
            for (var dimension = 0; dimension < dimensionCount; dimension++)
            {
                row[dimension] = parameterValueSets[dimension][indices[dimension]];
            }

            yield return row;

            // Advance the odometer from the rightmost dimension.
            var position = dimensionCount - 1;
            while (position >= 0)
            {
                if (++indices[position] < parameterValueSets[position].Count)
                {
                    break;
                }

                indices[position] = 0;
                position--;
            }

            // All dimensions wrapped back to zero: enumeration is complete.
            if (position < 0)
            {
                yield break;
            }
        }
    }
}
