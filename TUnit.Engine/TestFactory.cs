using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Exceptions;

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
        
        return new ExecutableTest
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
        var combinations = CartesianProduct(allDataSets);
        
        foreach (var combination in combinations)
        {
            var flattened = combination.SelectMany(x => x).ToArray();
            var displayText = GenerateArgumentsDisplayText(flattened);
            results.Add((flattened, displayText));
        }
        
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
            var instance = Activator.CreateInstance(metadata.TestClassType) 
                ?? throw new TestException($"Failed to create instance of {metadata.TestClassType}");
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
            throw new TestException($"No invoker or MethodInfo available for test {metadata.TestName}");
        }
        
        return instance => _testInvoker.InvokeTestMethod(instance, metadata.MethodInfo, arguments);
    }
    
    private TestLifecycleHooks CreateHooks(TestMetadata metadata)
    {
        return new TestLifecycleHooks
        {
            BeforeClass = CreateHookInvokers(metadata.Hooks.BeforeClass, requiresInstance: false),
            AfterClass = CreateHookInvokers(metadata.Hooks.AfterClass, requiresInstance: true),
            BeforeTest = CreateHookInvokers(metadata.Hooks.BeforeTest, requiresInstance: true),
            AfterTest = CreateHookInvokers(metadata.Hooks.AfterTest, requiresInstance: true)
        };
    }
    
    private Func<HookContext, Task>[] CreateHookInvokers(HookMetadata[] hooks, bool requiresInstance)
    {
        if (requiresInstance)
        {
            // These will be cast to Func<object, HookContext, Task> at usage
            return hooks
                .OrderBy(h => h.Order)
                .Select(h => CreateHookInvoker(h))
                .Cast<Func<HookContext, Task>>()
                .ToArray();
        }
        
        return hooks
            .OrderBy(h => h.Order)
            .Select(h => CreateHookInvoker(h))
            .ToArray();
    }
    
    private Func<HookContext, Task> CreateHookInvoker(HookMetadata hook)
    {
        if (hook.Invoker != null)
        {
            // AOT-safe path
            return context => hook.Invoker(null, context);
        }
        
        // Reflection fallback
        if (hook.MethodInfo == null)
        {
            throw new TestException($"No invoker or MethodInfo available for hook {hook.Name}");
        }
        
        return context => _hookInvoker.InvokeHook(hook, context);
    }
    
    private async Task InjectProperties(object instance, Dictionary<string, object?> propertyValues)
    {
        foreach (var (propName, value) in propertyValues)
        {
            var property = instance.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanWrite == true)
            {
                property.SetValue(instance, value);
            }
        }
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
    
    private static IEnumerable<List<object?[]>> CartesianProduct(List<IEnumerable<object?[]>> sets)
    {
        if (!sets.Any())
        {
            yield return new List<object?[]>();
            yield break;
        }
        
        var first = sets.First();
        var rest = sets.Skip(1).ToList();
        var restProduct = CartesianProduct(rest).ToList();
        
        foreach (var item in first)
        {
            foreach (var restItem in restProduct)
            {
                var result = new List<object?[]> { item };
                result.AddRange(restItem);
                yield return result;
            }
        }
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
        
        while (true)
        {
            // Create combination for current indices
            var combination = new Dictionary<string, object?>();
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var values = propertyDataMap[prop];
                combination[prop] = values[indices[i]];
            }
            results.Add(combination);
            
            // Increment indices
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
        }
        
        return results;
    }
}

/// <summary>
/// Resolves test data sources
/// </summary>
public interface IDataSourceResolver
{
    Task<IEnumerable<object?[]>> ResolveDataSource(TestDataSource dataSource);
}

/// <summary>
/// Invokes test methods
/// </summary>
public interface ITestInvoker
{
    Task InvokeTestMethod(object instance, MethodInfo method, object?[] arguments);
}

/// <summary>
/// Invokes hook methods
/// </summary>
public interface IHookInvoker
{
    Task InvokeHook(HookMetadata hook, HookContext context);
}