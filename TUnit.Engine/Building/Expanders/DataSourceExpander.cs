using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Expanders;

/// <summary>
/// Expands test data sources into executable test data
/// </summary>
public sealed class DataSourceExpander : IDataSourceExpander
{
    private const int DataSourceTimeoutSeconds = 30;

    public DataSourceExpander()
    {
        // No longer need dynamic data resolver - data sources contain their delegates directly
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExpandedTestData>> ExpandDataSourcesAsync(TestMetadata metadata)
    {
        var expanded = new List<ExpandedTestData>();

        // Get all data source combinations
        var combinations = await GetDataSourceCombinationsAsync(metadata, CancellationToken.None);

        // Create expanded test data for each combination
        foreach (var (classArgs, methodArgs, properties, indices) in combinations)
        {
            var expandedData = new ExpandedTestData
            {
                Metadata = metadata,
                DataSourceIndices = indices,
                ClassArgumentsFactory = classArgs,
                MethodArgumentsFactory = methodArgs,
                PropertyFactories = properties,
                ArgumentsDisplayText = GenerateDisplayText(classArgs(), methodArgs())
            };

            expanded.Add(expandedData);
        }

        // If no data sources, create a single test without arguments
        if (expanded.Count == 0)
        {
            expanded.Add(new ExpandedTestData
            {
                Metadata = metadata,
                DataSourceIndices = Array.Empty<int>(),
                ClassArgumentsFactory = () => Array.Empty<object?>(),
                MethodArgumentsFactory = () => Array.Empty<object?>(),
                PropertyFactories = new Dictionary<string, Func<object?>>(),
                ArgumentsDisplayText = string.Empty
            });
        }

        return expanded;
    }

    private async Task<List<(Func<object?[]> ClassArgs, Func<object?[]> MethodArgs, Dictionary<string, Func<object?>> Properties, int[] Indices)>>
        GetDataSourceCombinationsAsync(TestMetadata metadata, CancellationToken cancellationToken)
    {
        var combinations = new List<(Func<object?[]>, Func<object?[]>, Dictionary<string, Func<object?>>, int[])>();

        // Resolve all data sources
        var classDataFactories = await ResolveDataSourcesAsync(
            metadata.ClassDataSources,
            DataSourceLevel.Class,
            cancellationToken);

        var methodDataFactories = await ResolveDataSourcesAsync(
            metadata.DataSources,
            DataSourceLevel.Method,
            cancellationToken);

        var propertyDataFactories = await ResolvePropertyDataSourcesAsync(
            metadata.PropertyDataSources,
            cancellationToken);

        // Handle the case where there are no data sources
        if (!classDataFactories.Any() && !methodDataFactories.Any() && !propertyDataFactories.Any())
        {
            return combinations;
        }

        // Ensure we have at least one item in each collection
        if (!classDataFactories.Any())
        {
            classDataFactories = new List<IEnumerable<Func<object?[]>>> { Array.Empty<Func<object?[]>>() };
        }
        if (!methodDataFactories.Any())
        {
            methodDataFactories = new List<IEnumerable<Func<object?[]>>> { Array.Empty<Func<object?[]>>() };
        }
        if (!propertyDataFactories.Any())
        {
            propertyDataFactories = new List<Dictionary<string, List<Func<object?>>>> { new Dictionary<string, List<Func<object?>>>() };
        }

        // Generate all combinations
        var indices = new List<int>();
        var currentIndex = 0;

        foreach (var classData in classDataFactories)
        {
            foreach (var methodData in methodDataFactories)
            {
                foreach (var propData in propertyDataFactories)
                {
                    // Convert class data
                    var classArgsFactory = classData.Any()
                        ? () => ConvertToFactories(classData).First()()
                        : (Func<object?[]>)(() => Array.Empty<object?>());

                    // Convert method data
                    var methodArgsFactory = methodData.Any()
                        ? () => ConvertToFactories(methodData).First()()
                        : (Func<object?[]>)(() => Array.Empty<object?>());

                    // Convert property data
                    var propertyFactories = new Dictionary<string, Func<object?>>();
                    foreach (var kvp in propData)
                    {
                        if (kvp.Value.Any())
                        {
                            var factory = kvp.Value.First();
                            propertyFactories[kvp.Key] = factory;
                        }
                    }

                    indices.Add(currentIndex++);
                    combinations.Add((classArgsFactory, methodArgsFactory, propertyFactories, indices.ToArray()));
                    indices.Clear();
                }
            }
        }

        return combinations;
    }

    private async Task<List<IEnumerable<Func<object?[]>>>> ResolveDataSourcesAsync(
        TestDataSource[] dataSources,
        DataSourceLevel level,
        CancellationToken cancellationToken)
    {
        if (dataSources.Length == 0)
        {
            return new List<IEnumerable<Func<object?[]>>>();
        }

        var resolved = new List<IEnumerable<Func<object?[]>>>();

        foreach (var dataSource in dataSources)
        {
            var factories = await ResolveDataSourceAsync(dataSource, level, cancellationToken);
            resolved.Add(factories);
        }

        return resolved;
    }

    private async Task<IEnumerable<Func<object?[]>>> ResolveDataSourceAsync(
        TestDataSource dataSource,
        DataSourceLevel level,
        CancellationToken cancellationToken)
    {
        switch (dataSource)
        {
            case StaticTestDataSource staticSource:
                return staticSource.GetDataFactories();

            case DelegateDataSource delegateSource:
                return await Task.Run(() => delegateSource.GetDataFactories(), cancellationToken);

            case AsyncDelegateDataSource asyncSource:
                return await Task.Run(() => asyncSource.GetDataFactories(), cancellationToken);

            case TaskDelegateDataSource taskSource:
                return await Task.Run(() => taskSource.GetDataFactories(), cancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
            case DynamicTestDataSource:
                throw new NotSupportedException("DynamicTestDataSource is obsolete. Use DelegateDataSource instead.");

            case AsyncDynamicTestDataSource:
                throw new NotSupportedException("AsyncDynamicTestDataSource is obsolete. Use AsyncDelegateDataSource instead.");
#pragma warning restore CS0618 // Type or member is obsolete

            default:
                throw new NotSupportedException($"Data source type {dataSource.GetType()} is not supported");
        }
    }


    private async Task<List<Dictionary<string, List<Func<object?>>>>> ResolvePropertyDataSourcesAsync(
        PropertyDataSource[] propertyDataSources,
        CancellationToken cancellationToken)
    {
        if (propertyDataSources.Length == 0)
        {
            return new List<Dictionary<string, List<Func<object?>>>>();
        }

        var result = new Dictionary<string, List<Func<object?>>>();

        foreach (var propSource in propertyDataSources)
        {
            var factories = await ResolveDataSourceAsync(
                propSource.DataSource,
                DataSourceLevel.Property,
                cancellationToken);

            var propFactories = new List<Func<object?>>();
            foreach (var factory in factories)
            {
                propFactories.Add(() => factory()[0]); // Properties expect single values
            }

            result[propSource.PropertyName] = propFactories;
        }

        // For now, return a single dictionary wrapped in a list
        // TODO: Support multiple property value combinations
        return new List<Dictionary<string, List<Func<object?>>>>{ result };
    }

    private static string GenerateDisplayText(object?[] classArgs, object?[] methodArgs)
    {
        var allArgs = classArgs.Concat(methodArgs).ToArray();
        if (allArgs.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(", ", allArgs.Select(FormatArgument));
    }

    private static string FormatArgument(object? arg)
    {
        return arg switch
        {
            null => "null",
            string str => $"\"{str}\"",
            char ch => $"'{ch}'",
            bool b => b.ToString().ToLowerInvariant(),
            _ => arg.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Converts raw factories to the expected format
    /// </summary>
    private static IEnumerable<Func<object?[]>> ConvertToFactories(IEnumerable<Func<object?[]>> factories)
    {
        // Handle tuple unwrapping for AOT scenarios
        foreach (var factory in factories)
        {
            yield return () =>
            {
                var args = factory();
                return UnwrapTuples(args);
            };
        }
    }

    /// <summary>
    /// Unwraps tuples in arguments for proper parameter matching
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicFields' in call to 'System.Type.GetFields()'", Justification = "Tuple fields are always public and available")]
    private static object?[] UnwrapTuples(object?[] args)
    {
        if (args.Length == 1 && args[0] != null)
        {
            var argType = args[0]!.GetType();
            if (argType.IsValueType && argType.Name.StartsWith("ValueTuple`"))
            {
                // Use reflection to access tuple elements for compatibility
                var fields = argType.GetFields();
                if (fields.Length > 0)
                {
                    var unwrapped = new object?[fields.Length];
                    for (var i = 0; i < fields.Length; i++)
                    {
                        unwrapped[i] = fields[i].GetValue(args[0]);
                    }
                    return unwrapped;
                }
            }
        }

        return args;
    }
}

/// <summary>
/// Level at which a data source is applied
/// </summary>
public enum DataSourceLevel
{
    Class,
    Method,
    Property
}
