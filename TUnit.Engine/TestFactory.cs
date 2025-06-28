using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Exceptions;

using TUnit.Engine.Helpers;
using TUnit.Engine.Configuration;
namespace TUnit.Engine;

/// <summary>
/// Factory for creating executable tests from metadata, replacing multiple builder classes
/// </summary>
public sealed class TestFactory
{
    private readonly ITestInvoker _testInvoker;
    private readonly IHookInvoker _hookInvoker;
    private readonly IDataSourceResolver _dataSourceResolver;
    
    public TestFactory(
        ITestInvoker testInvoker,
        IHookInvoker hookInvoker,
        IDataSourceResolver dataSourceResolver)
    {
        _testInvoker = testInvoker;
        _hookInvoker = hookInvoker;
        _dataSourceResolver = dataSourceResolver;
    }
    
    /// <summary>
    /// Creates executable tests from metadata, expanding data-driven tests
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> CreateTests(TestMetadata metadata)
    {
        var tests = new List<ExecutableTest>();
        
        // Get all test data combinations
        var testDataSets = await ExpandTestData(metadata);
        
        // Get property data if any
        var propertyDataSets = await ExpandPropertyData(metadata);
        
        // Create executable test for each data combination
        foreach (var (arguments, argumentsDisplayText) in testDataSets)
        {
            foreach (var propertyData in propertyDataSets.DefaultIfEmpty(new Dictionary<string, object?>()))
            {
                var executableTest = CreateExecutableTest(metadata, arguments, argumentsDisplayText, propertyData);
                tests.Add(executableTest);
            }
        }
        
        return tests;
    }
    
    private ExecutableTest CreateExecutableTest(
        TestMetadata metadata,
        object?[] arguments,
        string argumentsDisplayText,
        Dictionary<string, object?> propertyValues)
    {
        var testId = GenerateTestId(metadata, arguments, propertyValues);
        var displayName = GenerateDisplayName(metadata, argumentsDisplayText, propertyValues);
        
        // Create instance factory
        var createInstance = CreateInstanceFactory(metadata, propertyValues);
        
        // Create test invoker
        var invokeTest = CreateTestInvoker(metadata, arguments);
        
        // Create hooks
        var hooks = CreateHooks(metadata);
        
        var executableTest = new ExecutableTest
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = arguments,
            CreateInstance = createInstance,
            InvokeTest = invokeTest,
            PropertyValues = propertyValues,
            Hooks = hooks
        };
        
        // Create test context for discovery
        executableTest.Context = CreateTestContext(executableTest);
        
        return executableTest;
    }
    
    private async Task<List<(object?[] arguments, string displayText)>> ExpandTestData(TestMetadata metadata)
    {
        var results = new List<(object?[], string)>();
        
        if (metadata.DataSources.Length == 0)
        {
            // No data sources, single test with no arguments
            results.Add((Array.Empty<object?>(), string.Empty));
            return results;
        }
        
        // Resolve all data sources
        var allDataSets = new List<IEnumerable<object?[]>>();
        foreach (var dataSource in metadata.DataSources)
        {
            var data = await _dataSourceResolver.ResolveDataSource(dataSource);
            allDataSets.Add(data);
        }
        
        // Generate cartesian product for matrix/combinatorial tests
        DiscoveryDiagnostics.RecordDataSourceStart($"{metadata.TestName}", allDataSets.Count);
        var combinations = CartesianProduct(allDataSets);
        
        foreach (var combination in combinations)
        {
            var flattened = combination.SelectMany(x => x).ToArray();
            var displayText = GenerateArgumentsDisplayText(flattened);
            results.Add((flattened, displayText));
        }
        
        DiscoveryDiagnostics.RecordTestExpansion(metadata.TestName, results.Count);
        DiscoveryDiagnostics.RecordDataSourceEnd($"{metadata.TestName}", results.Count);
        
        return results;
    }
    
    private async Task<List<Dictionary<string, object?>>> ExpandPropertyData(TestMetadata metadata)
    {
        var results = new List<Dictionary<string, object?>>();
        
        if (metadata.PropertyDataSources.Length == 0)
        {
            return results;
        }
        
        // Resolve property data sources
        var propertyDataMap = new Dictionary<string, List<object?>>();
        foreach (var propSource in metadata.PropertyDataSources)
        {
            var data = await _dataSourceResolver.ResolveDataSource(propSource.DataSource);
            var values = data.Select(args => args.Length > 0 ? args[0] : null).ToList();
            propertyDataMap[propSource.PropertyName] = values;
        }
        
        // Generate all combinations
        var combinations = GeneratePropertyCombinations(propertyDataMap);
        results.AddRange(combinations);
        
        return results;
    }
    
    private Func<Task<object>> CreateInstanceFactory(TestMetadata metadata, Dictionary<string, object?> propertyValues)
    {
        if (metadata.InstanceFactory != null)
        {
            // AOT-safe path
            return async () =>
            {
                var instance = metadata.InstanceFactory();
                await InjectProperties(instance, propertyValues);
                return instance;
            };
        }
        
        // Reflection fallback
        return async () =>
        {
            #pragma warning disable IL2072 // Test class types are known at compile time through source generation
            var instance = Activator.CreateInstance(metadata.TestClassType) 
                ?? throw new InvalidOperationException($"Failed to create instance of {metadata.TestClassType}");
            #pragma warning restore IL2072
            await InjectProperties(instance, propertyValues);
            return instance;
        };
    }
    
    private Func<object, Task> CreateTestInvoker(TestMetadata metadata, object?[] arguments)
    {
        if (metadata.TestInvoker != null)
        {
            // AOT-safe path
            return instance => metadata.TestInvoker(instance, arguments);
        }
        
        // Reflection fallback
        if (metadata.MethodInfo == null)
        {
            throw new InvalidOperationException($"No invoker or MethodInfo available for test {metadata.TestName}");
        }
        
        return instance => _testInvoker.InvokeTestMethod(instance, metadata.MethodInfo, arguments);
    }
    
    private TestLifecycleHooks CreateHooks(TestMetadata metadata)
    {
        return new TestLifecycleHooks
        {
            BeforeClass = CreateBeforeClassHookInvokers(metadata.Hooks.BeforeClass),
            AfterClass = CreateInstanceHookInvokers(metadata.Hooks.AfterClass),
            BeforeTest = CreateInstanceHookInvokers(metadata.Hooks.BeforeTest),
            AfterTest = CreateInstanceHookInvokers(metadata.Hooks.AfterTest)
        };
    }
    
    private Func<HookContext, Task>[] CreateBeforeClassHookInvokers(HookMetadata[] hooks)
    {
        return hooks.Select(h => new Func<HookContext, Task>(async context =>
        {
            if (h.Invoker != null)
            {
                await h.Invoker(null, context);
            }
            else if (h.MethodInfo != null)
            {
                await _hookInvoker.InvokeHookAsync(null, h.MethodInfo, context);
            }
        })).ToArray();
    }
    
    private Func<object, HookContext, Task>[] CreateInstanceHookInvokers(HookMetadata[] hooks)
    {
        return hooks.Select(h => new Func<object, HookContext, Task>(async (instance, context) =>
        {
            if (h.Invoker != null)
            {
                await h.Invoker(instance, context);
            }
            else if (h.MethodInfo != null)
            {
                await _hookInvoker.InvokeHookAsync(instance, h.MethodInfo, context);
            }
        })).ToArray();
    }
    
    
    private Task InjectProperties(object instance, Dictionary<string, object?> propertyValues)
    {
        foreach (var kvp in propertyValues)
        {
            #pragma warning disable IL2075 // Test instance types are known at compile time
            var property = instance.GetType().GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
            #pragma warning restore IL2075
            if (property?.CanWrite == true)
            {
                property.SetValue(instance, kvp.Value);
            }
        }
        return Task.CompletedTask;
    }
    
    private static string GenerateTestId(TestMetadata metadata, object?[] arguments, Dictionary<string, object?> propertyValues)
    {
        var parts = new List<string> { metadata.TestId };
        
        if (arguments.Length > 0)
        {
            parts.Add($"[{string.Join(",", arguments.Select(FormatArgument))}]");
        }
        
        if (propertyValues.Count > 0)
        {
            parts.Add($"<{string.Join(",", propertyValues.Select(kv => $"{kv.Key}={FormatArgument(kv.Value)}"))}>");
        }
        
        return string.Join("_", parts);
    }
    
    private static string GenerateDisplayName(TestMetadata metadata, string argumentsDisplayText, Dictionary<string, object?> propertyValues)
    {
        var displayName = metadata.TestName;
        
        if (!string.IsNullOrEmpty(argumentsDisplayText))
        {
            displayName += $"({argumentsDisplayText})";
        }
        
        if (propertyValues.Count > 0)
        {
            var propDisplay = string.Join(", ", propertyValues.Select(kv => $"{kv.Key}: {FormatArgument(kv.Value)}"));
            displayName += $" [{propDisplay}]";
        }
        
        return displayName;
    }
    
    private static string GenerateArgumentsDisplayText(object?[] arguments)
    {
        if (arguments.Length == 0) return string.Empty;
        return string.Join(", ", arguments.Select(FormatArgument));
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
    
    private TestContext CreateTestContext(ExecutableTest test)
    {
        // Create ClassMetadata
        var classMetadata = ClassMetadata.GetOrAdd(test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name, () => new ClassMetadata
        {
            Name = test.Metadata.TestClassType.Name,
            Type = test.Metadata.TestClassType,
            TypeReference = TypeReference.CreateConcrete(test.Metadata.TestClassType.AssemblyQualifiedName ?? test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name),
            Namespace = test.Metadata.TestClassType.Namespace,
            Assembly = AssemblyMetadata.GetOrAdd(test.Metadata.TestClassType.Assembly.FullName ?? "Unknown", () => new AssemblyMetadata
            {
                Name = test.Metadata.TestClassType.Assembly.GetName().Name ?? "Unknown",
                Attributes = Array.Empty<AttributeMetadata>()
            }),
            Parameters = Array.Empty<ParameterMetadata>(), // TODO: Get from constructor if needed
            Properties = Array.Empty<PropertyMetadata>(), // TODO: Get from type if needed
            Parent = null, // TODO: Get base type if needed
            Attributes = Array.Empty<AttributeMetadata>() // TODO: Get from type attributes if needed
        });
        
        // Create MethodMetadata
        MethodMetadata methodMetadata;
        if (test.Metadata.MethodInfo != null)
        {
            var methodInfo = test.Metadata.MethodInfo;
            methodMetadata = new MethodMetadata
            {
                Name = methodInfo.Name,
                Type = test.Metadata.TestClassType,
                TypeReference = TypeReference.CreateConcrete(test.Metadata.TestClassType.AssemblyQualifiedName ?? test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name),
                Class = classMetadata,
                #pragma warning disable IL2072 // ParameterType comes from test method parameters which are known at compile time
                Parameters = methodInfo.GetParameters().Select(p => new ParameterMetadata(p.ParameterType)
                #pragma warning restore IL2072
                {
                    Name = p.Name ?? "param" + p.Position,
                    TypeReference = TypeReference.CreateConcrete(p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName ?? p.ParameterType.Name),
                    Attributes = new AttributeMetadata[0],
                    ReflectionInfo = p
                }).ToArray(),
                GenericTypeCount = methodInfo.IsGenericMethodDefinition ? methodInfo.GetGenericArguments().Length : 0,
                ReturnTypeReference = TypeReference.CreateConcrete(methodInfo.ReturnType.AssemblyQualifiedName ?? methodInfo.ReturnType.FullName ?? methodInfo.ReturnType.Name),
                ReturnType = methodInfo.ReturnType,
                Attributes = Array.Empty<AttributeMetadata>() // TODO: Get from method attributes if needed
            };
        }
        else
        {
            // Create minimal MethodMetadata when MethodInfo is not available
            methodMetadata = new MethodMetadata
            {
                Name = test.Metadata.TestMethodName,
                Type = test.Metadata.TestClassType,
                TypeReference = TypeReference.CreateConcrete(test.Metadata.TestClassType.AssemblyQualifiedName ?? test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name),
                Class = classMetadata,
                Parameters = Array.Empty<ParameterMetadata>(),
                GenericTypeCount = 0,
                ReturnTypeReference = TypeReference.CreateConcrete(typeof(Task).AssemblyQualifiedName ?? typeof(Task).FullName ?? "System.Threading.Tasks.Task"),
                ReturnType = typeof(Task),
                Attributes = Array.Empty<AttributeMetadata>()
            };
        }
        
        var testDetails = new TestDetails
        {
            TestId = test.TestId,
            TestName = test.Metadata.TestName,
            ClassType = test.Metadata.TestClassType,
            MethodName = test.Metadata.TestMethodName,
            ClassInstance = null, // Will be set during execution
            TestMethodArguments = test.Arguments,
            TestClassArguments = Array.Empty<object?>(),
            DisplayName = test.DisplayName,
            TestFilePath = test.Metadata.FilePath ?? "Unknown",
            TestLineNumber = test.Metadata.LineNumber ?? 0,
            TestMethodParameterTypes = test.Metadata.ParameterTypes,
            ReturnType = test.Metadata.MethodInfo?.ReturnType ?? typeof(Task),
            ClassMetadata = classMetadata,
            MethodMetadata = methodMetadata
        };
        
        // Add categories
        foreach (var category in test.Metadata.Categories)
        {
            testDetails.Categories.Add(category);
        }
        
        var context = new TestContext(test.Metadata.TestName, test.DisplayName)
        {
            TestDetails = testDetails,
            CancellationToken = CancellationToken.None,
            InternalDiscoveredTest = null // Will be set by TestDiscoveryService when needed
        };
        
        return context;
    }
    
    private static IEnumerable<List<object?[]>> CartesianProduct(List<IEnumerable<object?[]>> sets)
    {
        return CartesianProductWithLimits(sets, 0, new CartesianProductState());
    }
    
    private static IEnumerable<List<object?[]>> CartesianProductWithLimits(
        List<IEnumerable<object?[]>> sets, 
        int depth,
        CartesianProductState state)
    {
        var maxDepth = DiscoveryConfiguration.MaxCartesianDepth;
        var maxTotalCombinations = DiscoveryConfiguration.MaxCartesianCombinations;
        
        // Record diagnostics
        DiscoveryDiagnostics.RecordCartesianProductDepth(depth, sets.Count);
        
        if (depth > maxDepth)
        {
            throw new InvalidOperationException(
                $"Cartesian product exceeded maximum recursion depth of {maxDepth}. " +
                "This may indicate an excessive number of data source combinations.");
        }
        
        if (!sets.Any())
        {
            yield return new List<object?[]>();
            yield break;
        }
        
        var first = sets.First();
        var rest = sets.Skip(1).ToList();
        var restProduct = CartesianProductWithLimits(rest, depth + 1, state).ToList();
        
        foreach (var item in first)
        {
            foreach (var restItem in restProduct)
            {
                state.TotalCombinations++;
                
                if (state.TotalCombinations > maxTotalCombinations)
                {
                    throw new InvalidOperationException(
                        $"Cartesian product exceeded maximum combinations limit of {maxTotalCombinations:N0}. " +
                        "Consider reducing the number of data sources or their sizes.");
                }
                
                var result = new List<object?[]> { item };
                result.AddRange(restItem);
                yield return result;
            }
        }
    }
    
    private class CartesianProductState
    {
        public int TotalCombinations { get; set; }
    }
    
    private static List<Dictionary<string, object?>> GeneratePropertyCombinations(Dictionary<string, List<object?>> propertyDataMap)
    {
        if (!propertyDataMap.Any())
        {
            return new List<Dictionary<string, object?>>();
        }
        
        var results = new List<Dictionary<string, object?>>();
        var properties = propertyDataMap.Keys.ToList();
        var indices = new int[properties.Count];
        
        const int maxIterations = 1_000_000;
        int iterationCount = 0;
        
        while (iterationCount < maxIterations)
        {
            var combination = new Dictionary<string, object?>();
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var values = propertyDataMap[prop];
                combination[prop] = values[indices[i]];
            }
            results.Add(combination);
            
            int position = properties.Count - 1;
            while (position >= 0)
            {
                indices[position]++;
                if (indices[position] < propertyDataMap[properties[position]].Count)
                {
                    break;
                }
                indices[position] = 0;
                position--;
            }
            
            if (position < 0) break;
            iterationCount++;
        }
        
        if (iterationCount >= maxIterations)
        {
            throw new InvalidOperationException($"Property data combination generation exceeded maximum iterations ({maxIterations}). This may indicate an infinite loop or excessively large data set.");
        }
        
        return results;
    }
}