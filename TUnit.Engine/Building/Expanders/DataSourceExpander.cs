using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Configuration;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Building.Expanders;

/// <summary>
/// Expands data sources into test variations
/// </summary>
public sealed class DataSourceExpander : IDataSourceExpander
{
    private readonly IDynamicDataSourceResolver _dynamicResolver;

    public DataSourceExpander(IDynamicDataSourceResolver dynamicResolver)
    {
        _dynamicResolver = dynamicResolver ?? throw new ArgumentNullException(nameof(dynamicResolver));
    }

    public async Task<IEnumerable<ExpandedTestData>> ExpandDataSourcesAsync(TestMetadata metadata)
    {
        var expandedTests = new List<ExpandedTestData>();

        // Expand class data sources
        var classDataFactories = await ExpandDataSourcesAsync(metadata.ClassDataSources, DataSourceLevel.Class);

        // Expand method data sources
        var methodDataFactories = await ExpandDataSourcesAsync(metadata.DataSources, DataSourceLevel.Method);

        // Expand property data sources
        var propertyFactories = await ExpandPropertyDataSourcesAsync(metadata.PropertyDataSources);

        // Generate cartesian product of all combinations
        var combinations = GenerateTestCombinations(
            classDataFactories,
            methodDataFactories,
            new List<Dictionary<string, List<Func<object?>>>> { propertyFactories });

        foreach (var combination in combinations)
        {
            var expandedData = new ExpandedTestData
            {
                Metadata = metadata,
                ClassArgumentsFactory = combination.ClassArgumentsFactory,
                MethodArgumentsFactory = combination.MethodArgumentsFactory,
                PropertyFactories = combination.PropertyFactories,
                ArgumentsDisplayText = combination.DisplayText,
                DataSourceIndices = combination.DataSourceIndices
            };

            expandedTests.Add(expandedData);
        }

        return expandedTests;
    }

    private async Task<List<DataSourceFactorySet>> ExpandDataSourcesAsync(
        TestDataSource[] dataSources,
        DataSourceLevel level)
    {
        if (dataSources.Length == 0)
        {
            // No data sources - single test with no arguments
            return new List<DataSourceFactorySet>
            {
                new DataSourceFactorySet
                {
                    Factories = new[] { () => Array.Empty<object?>() },
                    SourceIndex = -1
                }
            };
        }

        var allFactorySets = new List<DataSourceFactorySet>();

        for (int i = 0; i < dataSources.Length; i++)
        {
            var dataSource = dataSources[i];
            var factories = await GetDataFactoriesAsync(dataSource, level);

            allFactorySets.Add(new DataSourceFactorySet
            {
                Factories = factories.ToArray(),
                SourceIndex = i
            });
        }

        return allFactorySets;
    }

    private async Task<Dictionary<string, List<Func<object?>>>> ExpandPropertyDataSourcesAsync(
        PropertyDataSource[] propertyDataSources)
    {
        var propertyFactories = new Dictionary<string, List<Func<object?>>>();

        foreach (var propSource in propertyDataSources)
        {
            var factories = await GetDataFactoriesAsync(propSource.DataSource, DataSourceLevel.Property);

            // For properties, we only use the first value from each factory
            var propertyValueFactories = factories
                .Select(factory => new Func<object?>(() =>
                {
                    var args = factory();
                    return args.Length > 0 ? args[0] : null;
                }))
                .ToList();

            propertyFactories[propSource.PropertyName] = propertyValueFactories;
        }

        return propertyFactories;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "DynamicTestDataSource is only used as fallback when AOT-friendly resolution isn't possible. Source generator preferentially creates AotFriendlyTestDataSource.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "DynamicTestDataSource is only used as fallback when AOT-friendly resolution isn't possible. Source generator preferentially creates AotFriendlyTestDataSource.")]
    private async Task<IEnumerable<Func<object?[]>>> GetDataFactoriesAsync(
        TestDataSource dataSource,
        DataSourceLevel level)
    {
        try
        {
            if (dataSource is StaticTestDataSource)
            {
                // Static data sources can return their factories directly
                return dataSource.GetDataFactories();
            }

            if (dataSource is AotFriendlyTestDataSource aotFriendlySource)
            {
                // AOT-friendly sources use direct method invocation, no reflection needed
                return aotFriendlySource.GetDataFactories();
            }

            if (dataSource is DynamicTestDataSource dynamicSource)
            {
                // Dynamic sources need resolution - these are only used when AOT-friendly resolution isn't possible
                // The source generator now preferentially creates AotFriendlyTestDataSource when possible
                return await _dynamicResolver.ResolveAsync(dynamicSource, level);
            }

            // Handle other data source types (e.g., AttributeDataSource)
            return dataSource.GetDataFactories();
        }
        catch (Exception ex)
        {
            // If data source resolution fails, return a factory that captures the error
            // This allows the test to still be discovered but fail when executed
            var errorMessage = $"Failed to resolve data source: {ex.Message}";
            return new[]
            {
                new Func<object?[]>(() => throw new InvalidOperationException(errorMessage, ex))
            };
        }
    }

    private IEnumerable<TestCombination> GenerateTestCombinations(
        List<DataSourceFactorySet> classDataSets,
        List<DataSourceFactorySet> methodDataSets,
        List<Dictionary<string, List<Func<object?>>>> propertyDataSets)
    {
        var combinations = new List<TestCombination>();

        // Generate cartesian product
        var classProduct = CartesianProduct(classDataSets);
        var methodProduct = CartesianProduct(methodDataSets);
        var propertyProduct = propertyDataSets.Any()
            ? GeneratePropertyCombinations(propertyDataSets.First())
            : new[] { new Dictionary<string, Func<object?>>() };

        foreach (var (classFactories, classIndices) in classProduct)
        {
            foreach (var (methodFactories, methodIndices) in methodProduct)
            {
                foreach (var propertyFactories in propertyProduct)
                {
                    // Combine all factories into a single factory for each level
                    var classFactory = CombineFactories(classFactories);
                    var methodFactory = CombineFactories(methodFactories);

                    // Generate display text
                    string displayText;
                    try
                    {
                        displayText = GenerateDisplayText(classFactory(), methodFactory());
                    }
                    catch (Exception ex)
                    {
                        // If we can't generate display text, use a fallback
                        displayText = $"[Data Source Error: {ex.Message}]";
                        
                        // Replace the factories with ones that throw the error
                        var errorMessage = $"Data source failed during test discovery: {ex.Message}";
                        classFactory = () => throw new InvalidOperationException(errorMessage, ex);
                        methodFactory = () => throw new InvalidOperationException(errorMessage, ex);
                    }

                    // Combine indices
                    var indices = CombineIndices(classIndices, methodIndices);

                    combinations.Add(new TestCombination
                    {
                        ClassArgumentsFactory = classFactory,
                        MethodArgumentsFactory = methodFactory,
                        PropertyFactories = propertyFactories,
                        DisplayText = displayText,
                        DataSourceIndices = indices
                    });
                }
            }
        }

        return combinations;
    }

    private IEnumerable<(List<Func<object?[]>> factories, List<int> indices)> CartesianProduct(
        List<DataSourceFactorySet> dataSets)
    {
        if (!dataSets.Any())
        {
            yield return (new List<Func<object?[]>>(), new List<int>());
            yield break;
        }

        var maxCombinations = DiscoveryConfiguration.MaxCartesianCombinations;
        var combinationCount = 0;

        // Simple implementation - can be optimized
        var indices = new int[dataSets.Count];
        var done = false;

        while (!done)
        {
            combinationCount++;
            if (combinationCount > maxCombinations)
            {
                throw new InvalidOperationException(
                    $"Cartesian product exceeded maximum combinations limit of {maxCombinations:N0}");
            }

            // Build current combination
            var factories = new List<Func<object?[]>>();
            var currentIndices = new List<int>();

            for (int i = 0; i < dataSets.Count; i++)
            {
                factories.Add(dataSets[i].Factories[indices[i]]);
                currentIndices.Add(dataSets[i].SourceIndex);
                currentIndices.Add(indices[i]);
            }

            yield return (factories, currentIndices);

            // Increment indices
            for (int i = dataSets.Count - 1; i >= 0; i--)
            {
                indices[i]++;
                if (indices[i] < dataSets[i].Factories.Length)
                {
                    break;
                }
                indices[i] = 0;
                if (i == 0)
                {
                    done = true;
                }
            }
        }
    }

    private IEnumerable<Dictionary<string, Func<object?>>> GeneratePropertyCombinations(
        Dictionary<string, List<Func<object?>>> propertyDataMap)
    {
        if (!propertyDataMap.Any())
        {
            yield return new Dictionary<string, Func<object?>>();
            yield break;
        }

        var properties = propertyDataMap.Keys.ToList();
        var indices = new int[properties.Count];
        var done = false;

        while (!done)
        {
            var combination = new Dictionary<string, Func<object?>>();

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var factories = propertyDataMap[prop];
                combination[prop] = factories[indices[i]];
            }

            yield return combination;

            // Increment indices
            for (int i = properties.Count - 1; i >= 0; i--)
            {
                indices[i]++;
                if (indices[i] < propertyDataMap[properties[i]].Count)
                {
                    break;
                }
                indices[i] = 0;
                if (i == 0)
                {
                    done = true;
                }
            }
        }
    }

    private static Func<object?[]> CombineFactories(List<Func<object?[]>> factories)
    {
        return () =>
        {
            var allArgs = new List<object?>();
            foreach (var factory in factories)
            {
                allArgs.AddRange(factory());
            }
            return allArgs.ToArray();
        };
    }

    private static string GenerateDisplayText(object?[] classArgs, object?[] methodArgs)
    {
        var parts = new List<string>();

        if (classArgs.Length > 0)
        {
            parts.Add(FormatArguments(classArgs));
        }

        if (methodArgs.Length > 0)
        {
            parts.Add(FormatArguments(methodArgs));
        }

        return string.Join(", ", parts);
    }

    private static string FormatArguments(object?[] args)
    {
        return string.Join(", ", args.Select(FormatArgument));
    }

    private static string FormatArgument(object? arg)
    {
        return arg switch
        {
            null => "null",
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            bool b => b.ToString().ToLower(),
            _ => arg.ToString() ?? "null"
        };
    }

    private static int[] CombineIndices(List<int> classIndices, List<int> methodIndices)
    {
        var combined = new List<int>(classIndices);
        combined.AddRange(methodIndices);
        return combined.ToArray();
    }

    private class DataSourceFactorySet
    {
        public required Func<object?[]>[] Factories { get; init; }
        public required int SourceIndex { get; init; }
    }

    private class TestCombination
    {
        public required Func<object?[]> ClassArgumentsFactory { get; init; }
        public required Func<object?[]> MethodArgumentsFactory { get; init; }
        public required Dictionary<string, Func<object?>> PropertyFactories { get; init; }
        public required string DisplayText { get; init; }
        public required int[] DataSourceIndices { get; init; }
    }
}

/// <summary>
/// Interface for resolving dynamic data sources
/// </summary>
public interface IDynamicDataSourceResolver
{
    [RequiresDynamicCode("Dynamic data source resolution requires reflection")]
    [RequiresUnreferencedCode("Dynamic data source resolution may access types not preserved by trimming")]
    Task<IEnumerable<Func<object?[]>>> ResolveAsync(DynamicTestDataSource dataSource, DataSourceLevel level);
}

/// <summary>
/// Implementation of dynamic data source resolver
/// </summary>
public sealed class DynamicDataSourceResolver : IDynamicDataSourceResolver
{
    [RequiresDynamicCode("Dynamic data source resolution requires reflection")]
    [RequiresUnreferencedCode("Dynamic data source resolution may access types not preserved by trimming")]
    public async Task<IEnumerable<Func<object?[]>>> ResolveAsync(DynamicTestDataSource dataSource, DataSourceLevel level)
    {
        const int DataSourceTimeoutSeconds = 30;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DataSourceTimeoutSeconds));

        try
        {
            return await ResolveWithTimeoutAsync(dataSource, level, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Data source '{dataSource.SourceMemberName}' on type '{dataSource.SourceType.FullName}' " +
                $"timed out after {DataSourceTimeoutSeconds} seconds.");
        }
    }

    [RequiresDynamicCode("Dynamic data source resolution requires reflection")]
    [RequiresUnreferencedCode("Dynamic data source resolution may access types not preserved by trimming")]
    private async Task<IEnumerable<Func<object?[]>>> ResolveWithTimeoutAsync(
        DynamicTestDataSource dataSource,
        DataSourceLevel level,
        CancellationToken cancellationToken)
    {
        // Find the member
        var member = dataSource.SourceType.GetMember(
            dataSource.SourceMemberName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .FirstOrDefault();

        if (member == null)
        {
            throw new InvalidOperationException(
                $"Could not find member '{dataSource.SourceMemberName}' on type '{dataSource.SourceType.FullName}'");
        }

        // Create instance if needed
        object? instance = null;
        if (!dataSource.IsShared && member is not MethodInfo { IsStatic: true })
        {
            instance = Activator.CreateInstance(dataSource.SourceType);
        }

        // Get the raw data
        var rawDataTask = Task.Run(() => member switch
        {
            PropertyInfo property => property.GetValue(instance),
            MethodInfo method => method.Invoke(instance, dataSource.Arguments),
            FieldInfo field => field.GetValue(instance),
            _ => throw new InvalidOperationException($"Unsupported member type: {member.GetType().Name}")
        }, cancellationToken);

        var rawData = await rawDataTask.ConfigureAwait(false);

        // Handle async results
        if (rawData is Task task)
        {
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            rawData = resultProperty?.GetValue(task);
        }

        // Convert to factory functions
        return ConvertToFactories(rawData, dataSource.SourceMemberName);
    }

    private IEnumerable<Func<object?[]>> ConvertToFactories(object? rawData, string sourceName)
    {
        var factories = new List<Func<object?[]>>();

        // Handle IDataSource implementations
        if (rawData is IDataSource dataSource)
        {
            var context = new DataSourceContext(
                GetType(), // Should be test class type
                DataSourceLevel.Method,
                null, null, null, null);
            return dataSource.GenerateDataFactories(context);
        }

        // Handle collections of factories
        if (rawData is IEnumerable<Func<object?[]>> funcEnumerable)
        {
            return funcEnumerable;
        }

        // Handle regular collections
        if (rawData is string stringValue)
        {
            // Special case: strings should be treated as single values
            factories.Add(() => new[] { stringValue });
            return factories;
        }

        if (rawData is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is object?[] array)
                {
                    // Create factory that returns a copy of the array
                    var capturedArray = array;
                    factories.Add(() =>
                    {
                        var copy = new object?[capturedArray.Length];
                        Array.Copy(capturedArray, copy, capturedArray.Length);
                        return copy;
                    });
                }
                else
                {
                    // Single value - wrap in array
                    var capturedItem = item;
                    factories.Add(() => new[] { capturedItem });
                }
            }
            return factories;
        }

        throw new InvalidOperationException(
            $"Data source '{sourceName}' did not return a valid collection or IDataSource implementation");
    }
}
