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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? testClassType = null)
    {
        // Extract data sources from metadata
        var methodDataSources = testMetadata.DataSources ?? Array.Empty<IDataSourceAttribute>();
        var classDataSources = testMetadata.ClassDataSources ?? Array.Empty<IDataSourceAttribute>();
        var propertyDataSources = testMetadata.PropertyDataSources ?? Array.Empty<PropertyDataSource>();
        
        // Get repeat count (default to 1 if no repeat attribute)
        var repeatCount = Math.Max(1, testMetadata.RepeatCount);
        
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? testClassType)
    {
        // Handle typed data sources that infer generic types
        var resolvedGenericTypes = new Dictionary<string, Type>();
        if (testMetadata.GenericTypeInfo != null && methodDataSources.Any(ds => ds.GetType().IsGenericType))
        {
            // Extract generic type arguments from typed data sources
            foreach (var dataSource in methodDataSources)
            {
                var dsType = dataSource.GetType();
                if (dsType.IsGenericType && dsType.GetGenericTypeDefinition().Name.StartsWith("TypedDataSourceAttribute"))
                {
                    var typeArg = dsType.GetGenericArguments()[0];
                    // Map to generic parameter - this is simplified, real implementation would need proper mapping
                    if (testMetadata.GenericTypeInfo.ParameterNames.Length > 0)
                    {
                        resolvedGenericTypes[testMetadata.GenericTypeInfo.ParameterNames[0]] = typeArg;
                    }
                }
            }
        }
        
        // Create data generator metadata for data sources
        var dataGeneratorMetadata = CreateDataGeneratorMetadata(testMetadata, testSessionId, testClassType);
        
        // Get method data
        var methodDataLists = new List<List<Func<Task<object?[]?>>>>();
        foreach (var dataSource in methodDataSources)
        {
            var dataList = new List<Func<Task<object?[]?>>>();
            await foreach (var data in dataSource.GetDataRowsAsync(dataGeneratorMetadata))
            {
                dataList.Add(data);
            }
            methodDataLists.Add(dataList);
        }
        
        // Get class data
        var classDataLists = new List<List<Func<Task<object?[]?>>>>();
        foreach (var dataSource in classDataSources)
        {
            var dataList = new List<Func<Task<object?[]?>>>();
            await foreach (var data in dataSource.GetDataRowsAsync(dataGeneratorMetadata))
            {
                dataList.Add(data);
            }
            classDataLists.Add(dataList);
        }
        
        // Generate combinations
        var methodCombinations = GenerateCombinations(methodDataLists);
        var classCombinations = GenerateCombinations(classDataLists);
        
        // If no data sources, create single empty combination
        if (!methodCombinations.Any() && !classCombinations.Any())
        {
            yield return new TestDataCombination
            {
                DisplayName = testMetadata.TestName,
                ResolvedGenericTypes = resolvedGenericTypes
            };
            yield break;
        }
        
        // Combine class and method data
        foreach (var classCombo in classCombinations.DefaultIfEmpty())
        {
            foreach (var methodCombo in methodCombinations.DefaultIfEmpty())
            {
                var displayNameParts = new List<string> { testMetadata.TestName };
                
                // Add class data to display name
                if (classCombo != null)
                {
                    var classData = await GetDataForDisplay(classCombo.Factories);
                    if (classData.Any())
                    {
                        displayNameParts.Add($"Class({string.Join(", ", classData.Select(FormatValue))})");
                    }
                }
                
                // Add method data to display name
                if (methodCombo != null)
                {
                    var methodData = await GetDataForDisplay(methodCombo.Factories);
                    if (methodData.Any())
                    {
                        displayNameParts.Add($"({string.Join(", ", methodData.Select(FormatValue))})");
                    }
                }
                
                yield return new TestDataCombination
                {
                    ClassDataFactories = classCombo?.Factories ?? Array.Empty<Func<Task<object?>>>(),
                    MethodDataFactories = methodCombo?.Factories ?? Array.Empty<Func<Task<object?>>>(),
                    ClassDataSourceIndex = classCombo?.DataSourceIndex ?? 0,
                    MethodDataSourceIndex = methodCombo?.DataSourceIndex ?? 0,
                    ClassLoopIndex = classCombo?.LoopIndex ?? 0,
                    MethodLoopIndex = methodCombo?.LoopIndex ?? 0,
                    DisplayName = string.Join(" - ", displayNameParts),
                    ResolvedGenericTypes = resolvedGenericTypes
                };
            }
        }
    }
    
    
    private static DataGeneratorMetadata CreateDataGeneratorMetadata(
        TestMetadata testMetadata,
        string testSessionId,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? testClassType)
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
    
    private static IEnumerable<DataCombination> GenerateCombinations(List<List<Func<Task<object?[]?>>>> dataLists)
    {
        if (!dataLists.Any() || dataLists.All(list => !list.Any()))
        {
            yield break;
        }
        
        var indices = new int[dataLists.Count];
        var dataSourceIndex = 0;
        
        while (true)
        {
            // Create current combination
            var factories = new List<Func<Task<object?>>>();
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
                }
            }
            
            yield return new DataCombination
            {
                Factories = factories.ToArray(),
                DataSourceIndex = dataSourceIndex,
                LoopIndex = indices[0] // Use first index as loop index
            };
            
            // Increment indices
            var carry = 1;
            for (int i = dataLists.Count - 1; i >= 0 && carry > 0; i--)
            {
                if (!dataLists[i].Any()) continue;
                
                indices[i] += carry;
                if (indices[i] >= dataLists[i].Count)
                {
                    indices[i] = 0;
                    dataSourceIndex++;
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
    
    private class DataCombination
    {
        public required Func<Task<object?>>[] Factories { get; init; }
        public required int DataSourceIndex { get; init; }
        public required int LoopIndex { get; init; }
    }
}