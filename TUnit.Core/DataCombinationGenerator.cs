using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// Runtime service for generating test data combinations from data sources
/// </summary>
public static class DataCombinationGenerator
{
    /// <summary>
    /// Generate all data combinations for a test at runtime
    /// </summary>
    public static async IAsyncEnumerable<TestDataCombination> GenerateCombinationsAsync(
        TestMetadata testMetadata,
        string testSessionId,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type testClassType)
    {
        // Extract data sources from metadata
        var methodDataSources = testMetadata.DataSources;
        var classDataSources = testMetadata.ClassDataSources;
        var propertyDataSources = testMetadata.PropertyDataSources;

        // Get repeat count (default to 1 if no repeat attribute)
        var repeatCount = testMetadata.RepeatCount + 1;

        // Generate combinations for all data sources uniformly
        await foreach (var combination in GenerateCombinationsForDataSourcesAsync(
            methodDataSources,
            classDataSources,
            propertyDataSources,
            testMetadata,
            testSessionId,
            testClassType))
        {
            for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
            {
                yield return new TestDataCombination
                {
                    ClassDataFactories = combination.ClassDataFactories,
                    MethodDataFactories = combination.MethodDataFactories,
                    ClassDataSourceIndex = combination.ClassDataSourceIndex,
                    MethodDataSourceIndex = combination.MethodDataSourceIndex,
                    ClassLoopIndex = combination.ClassLoopIndex,
                    MethodLoopIndex = combination.MethodLoopIndex,
                    DisplayName = combination.DisplayName,
                    RepeatIndex = repeatIndex,
                    ResolvedGenericTypes = combination.ResolvedGenericTypes
                };
            }
        }
    }

    private static async IAsyncEnumerable<TestDataCombination> GenerateCombinationsForDataSourcesAsync(
        IDataSourceAttribute[] methodDataSources,
        IDataSourceAttribute[] classDataSources,
        PropertyDataSource[] propertyDataSources,
        TestMetadata testMetadata,
        string testSessionId,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type testClassType)
    {
        // Handle typed data sources that infer generic types
        var resolvedGenericTypes = ResolveGenericTypesFromDataSources(
            testMetadata,
            methodDataSources,
            classDataSources,
            propertyDataSources);

        // Create data generator metadata for data sources
        var dataGeneratorMetadata = CreateDataGeneratorMetadata(testMetadata, testSessionId, testClassType);

        // Get method data
        var methodDataLists = await CollectDataSourceFactoriesAsync(methodDataSources, dataGeneratorMetadata);

        // Get class data
        var classDataLists = await CollectDataSourceFactoriesAsync(classDataSources, dataGeneratorMetadata);

        // Note: Property data sources are handled separately by PropertyInjector
        // and don't participate in combination generation at this time

        // Generate combinations
        var methodCombinations = GenerateCombinations(methodDataLists, DataSourceType.Method).ToArray();
        var classCombinations = GenerateCombinations(classDataLists, DataSourceType.Class).ToArray();

        // If no data sources at all, create single empty combination
        if (methodCombinations.Length == 0 && classCombinations.Length == 0)
        {
            yield return new TestDataCombination
            {
                DisplayName = testMetadata.TestName,
                ResolvedGenericTypes = resolvedGenericTypes
            };
            yield break;
        }

        // Combine all data sources
        // DefaultIfEmpty() ensures we handle cases where only one type has data
        foreach (var classCombo in classCombinations.DefaultIfEmpty())
        {
            foreach (var methodCombo in methodCombinations.DefaultIfEmpty())
            {
                var displayName = await GenerateDisplayNameAsync(
                    testMetadata,
                    classCombo,
                    methodCombo);

                yield return new TestDataCombination
                {
                    ClassDataFactories = classCombo?.Factories ?? [],
                    MethodDataFactories = methodCombo?.Factories ?? [],
                    ClassDataSourceIndex = classCombo?.DataSourceIndex ?? 0,
                    MethodDataSourceIndex = methodCombo?.DataSourceIndex ?? 0,
                    ClassLoopIndex = classCombo?.LoopIndex ?? 0,
                    MethodLoopIndex = methodCombo?.LoopIndex ?? 0,
                    DisplayName = displayName,
                    ResolvedGenericTypes = resolvedGenericTypes
                };
            }
        }
    }


    private static DataGeneratorMetadata CreateDataGeneratorMetadata(
        TestMetadata testMetadata,
        string testSessionId,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type testClassType)
    {
        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
            MembersToGenerate = [],
            TestInformation = testMetadata.MethodMetadata,
            Type = DataGeneratorType.TestParameters,
            TestSessionId = testSessionId,
            TestClassInstance = null,
            ClassInstanceArguments = null
        };
    }

    private static IEnumerable<DataCombination> GenerateCombinations(
        List<List<Func<Task<object?[]?>>>> dataLists,
        DataSourceType sourceType)
    {
        if (!dataLists.Any() || dataLists.All(list => !list.Any()))
        {
            yield break;
        }

        var indices = new int[dataLists.Count];
        var loopIndex = 0;

        while (true)
        {
            // Create current combination
            var factories = new List<Func<Task<object?>>>();
            var currentDataSourceIndex = 0;

            for (int i = 0; i < dataLists.Count; i++)
            {
                if (dataLists[i].Any())
                {
                    var dataFactory = dataLists[i][indices[i]];
                    factories.Add(async () =>
                    {
                        var data = await dataFactory();
                        return data?.FirstOrDefault();
                    });

                    // Track which data source we're on
                    if (indices[i] > 0 || i == 0)
                    {
                        currentDataSourceIndex = i;
                    }
                }
            }

            yield return new DataCombination
            {
                Factories = factories.ToArray(),
                DataSourceIndex = currentDataSourceIndex,
                LoopIndex = loopIndex,
                SourceType = sourceType
            };

            loopIndex++;

            // Increment indices
            var carry = 1;
            for (int i = dataLists.Count - 1; i >= 0 && carry > 0; i--)
            {
                if (!dataLists[i].Any()) continue;

                indices[i] += carry;
                if (indices[i] >= dataLists[i].Count)
                {
                    indices[i] = 0;
                }
                else
                {
                    carry = 0;
                }
            }

            if (carry > 0) break; // All combinations generated
        }
    }

    private static async Task<object?[]> GetDataForDisplay(Func<Task<object?>>[] factories)
    {
        var results = new List<object?>();
        foreach (var factory in factories)
        {
            results.Add(await factory());
        }
        return results.ToArray();
    }

    private static string FormatValue(object? value)
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        if (value is char ch) return $"'{ch}'";
        if (value is Type type) return type.Name;
        return value.ToString() ?? "null";
    }

    private static Dictionary<string, Type> ResolveGenericTypesFromDataSources(
        TestMetadata testMetadata,
        IDataSourceAttribute[] methodDataSources,
        IDataSourceAttribute[] classDataSources,
        PropertyDataSource[] propertyDataSources)
    {
        var resolvedTypes = new Dictionary<string, Type>();

        if (testMetadata.GenericTypeInfo == null && testMetadata.GenericMethodInfo == null)
        {
            return resolvedTypes;
        }

        // Process all data sources to find typed ones
        var allDataSources = methodDataSources
            .Concat(classDataSources);

        foreach (var dataSource in allDataSources)
        {
            var dsType = dataSource.GetType();
            if (!dsType.IsGenericType) continue;

            var genericDef = dsType.GetGenericTypeDefinition();
            if (genericDef.Name.StartsWith("TypedDataSourceAttribute"))
            {
                var typeArgs = dsType.GetGenericArguments();

                // Map generic type arguments based on position and constraints
                MapGenericTypeArguments(
                    testMetadata.GenericTypeInfo,
                    testMetadata.GenericMethodInfo,
                    typeArgs,
                    resolvedTypes);
            }
        }

        return resolvedTypes;
    }

    private static void MapGenericTypeArguments(
        GenericTypeInfo? classGenericInfo,
        GenericMethodInfo? methodGenericInfo,
        Type[] typeArguments,
        Dictionary<string, Type> resolvedTypes)
    {
        var typeArgIndex = 0;

        // First map method generic parameters
        if (methodGenericInfo != null)
        {
            for (int i = 0; i < methodGenericInfo.ParameterNames.Length && typeArgIndex < typeArguments.Length; i++)
            {
                var paramName = methodGenericInfo.ParameterNames[i];
                if (!resolvedTypes.ContainsKey(paramName))
                {
                    resolvedTypes[paramName] = typeArguments[typeArgIndex++];
                }
            }
        }

        // Then map class generic parameters
        if (classGenericInfo != null)
        {
            for (int i = 0; i < classGenericInfo.ParameterNames.Length && typeArgIndex < typeArguments.Length; i++)
            {
                var paramName = classGenericInfo.ParameterNames[i];
                if (!resolvedTypes.ContainsKey(paramName))
                {
                    resolvedTypes[paramName] = typeArguments[typeArgIndex++];
                }
            }
        }
    }

    private static async Task<List<List<Func<Task<object?[]?>>>>> CollectDataSourceFactoriesAsync(
        IDataSourceAttribute[] dataSources,
        DataGeneratorMetadata metadata)
    {
        var dataLists = new List<List<Func<Task<object?[]?>>>>();

        foreach (var dataSource in dataSources)
        {
            var dataList = new List<Func<Task<object?[]?>>>();
            await foreach (var data in dataSource.GetDataRowsAsync(metadata))
            {
                dataList.Add(data);
            }
            if (dataList.Any())
            {
                dataLists.Add(dataList);
            }
        }

        return dataLists;
    }

    private static async Task<string> GenerateDisplayNameAsync(
        TestMetadata testMetadata,
        DataCombination? classCombo,
        DataCombination? methodCombo)
    {
        var parts = new List<string> { testMetadata.TestName };

        // Add class data
        if (classCombo != null)
        {
            var classData = await GetDataForDisplay(classCombo.Factories);
            if (classData.Any())
            {
                parts.Add($"Class({string.Join(", ", classData.Select(FormatValue))})");
            }
        }

        // Add method data
        if (methodCombo != null)
        {
            var methodData = await GetDataForDisplay(methodCombo.Factories);
            if (methodData.Any())
            {
                parts.Add($"({string.Join(", ", methodData.Select(FormatValue))})");
            }
        }

        return string.Join(" - ", parts);
    }

    private enum DataSourceType
    {
        Class,
        Method
    }

    private class DataCombination
    {
        public required Func<Task<object?>>[] Factories { get; init; }
        public required int DataSourceIndex { get; init; }
        public required int LoopIndex { get; init; }
        public DataSourceType SourceType { get; init; }
    }
}
