using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace TUnit.Core;

/// <summary>
/// Unified metadata for a test, supporting both AOT (via delegates) and reflection scenarios
/// </summary>
public sealed class TestMetadata
{
    /// <summary>
    /// Unique identifier for the test
    /// </summary>
    public required string TestId { get; init; }
    
    /// <summary>
    /// Display name for the test
    /// </summary>
    public required string TestName { get; init; }
    
    /// <summary>
    /// The type containing the test method
    /// </summary>
    public required Type TestClassType { get; init; }
    
    /// <summary>
    /// The test method name
    /// </summary>
    public required string TestMethodName { get; init; }
    
    /// <summary>
    /// Test categories for filtering
    /// </summary>
    public string[] Categories { get; init; } = [];
    
    /// <summary>
    /// Whether this test should be skipped
    /// </summary>
    public bool IsSkipped { get; init; }
    
    /// <summary>
    /// Skip reason if IsSkipped is true
    /// </summary>
    public string? SkipReason { get; init; }
    
    /// <summary>
    /// Test timeout in milliseconds (null for no timeout)
    /// </summary>
    public int? TimeoutMs { get; init; }
    
    /// <summary>
    /// Number of retry attempts allowed
    /// </summary>
    public int RetryCount { get; init; }
    
    /// <summary>
    /// Whether this test can run in parallel
    /// </summary>
    public bool CanRunInParallel { get; init; } = true;
    
    /// <summary>
    /// Tests that must run before this one
    /// </summary>
    public string[] DependsOn { get; init; } = [];
    
    /// <summary>
    /// Test data for parameterized tests
    /// </summary>
    public TestDataSource[] DataSources { get; init; } = [];
    
    /// <summary>
    /// Properties that require data injection
    /// </summary>
    public PropertyDataSource[] PropertyDataSources { get; init; } = [];
    
    /// <summary>
    /// AOT-safe factory to create test class instance (null for reflection-based)
    /// </summary>
    public Func<object>? InstanceFactory { get; init; }
    
    /// <summary>
    /// AOT-safe test method invoker (null for reflection-based)
    /// Returns Task, ValueTask, or void (as Task.CompletedTask)
    /// </summary>
    public Func<object, object?[], Task>? TestInvoker { get; init; }
    
    /// <summary>
    /// Number of parameters the test method expects
    /// </summary>
    public int ParameterCount { get; init; }
    
    /// <summary>
    /// Parameter types for validation
    /// </summary>
    public Type[] ParameterTypes { get; init; } = [];
    
    /// <summary>
    /// Hooks to run at various test lifecycle points
    /// </summary>
    public TestHooks Hooks { get; init; } = new();
    
    /// <summary>
    /// For reflection scenarios - the MethodInfo (when AOT invoker not available)
    /// </summary>
    public MethodInfo? MethodInfo { get; init; }
    
    /// <summary>
    /// Source file path where test is defined
    /// </summary>
    public string? FilePath { get; init; }
    
    /// <summary>
    /// Line number where test is defined
    /// </summary>
    public int? LineNumber { get; init; }
}

/// <summary>
/// Represents a source of test data
/// </summary>
public abstract class TestDataSource
{
    /// <summary>
    /// Gets the test data items
    /// </summary>
    public abstract IEnumerable<object?[]> GetData();
}

/// <summary>
/// Static test data (e.g., from [Arguments] attribute)
/// </summary>
public sealed class StaticTestDataSource : TestDataSource
{
    private readonly object?[][] _data;
    
    public StaticTestDataSource(params object?[][] data)
    {
        _data = data;
    }
    
    public override IEnumerable<object?[]> GetData() => _data;
}

/// <summary>
/// Dynamic test data (e.g., from method or property)
/// </summary>
public sealed class DynamicTestDataSource : TestDataSource
{
    public required Type SourceType { get; init; }
    public required string SourceMemberName { get; init; }
    public bool IsShared { get; init; }
    
    public override IEnumerable<object?[]> GetData()
    {
        // This will be implemented by TestFactory
        throw new NotImplementedException("Dynamic data sources are resolved by TestFactory");
    }
}

/// <summary>
/// Property that requires data injection
/// </summary>
public sealed class PropertyDataSource
{
    public required string PropertyName { get; init; }
    public required Type PropertyType { get; init; }
    public required TestDataSource DataSource { get; init; }
}

/// <summary>
/// Test lifecycle hooks
/// </summary>
public sealed class TestHooks
{
    /// <summary>
    /// Hooks to run before the test class is instantiated
    /// </summary>
    public HookMetadata[] BeforeClass { get; init; } = [];
    
    /// <summary>
    /// Hooks to run after the test class is instantiated
    /// </summary>
    public HookMetadata[] AfterClass { get; init; } = [];
    
    /// <summary>
    /// Hooks to run before each test
    /// </summary>
    public HookMetadata[] BeforeTest { get; init; } = [];
    
    /// <summary>
    /// Hooks to run after each test
    /// </summary>
    public HookMetadata[] AfterTest { get; init; } = [];
}

/// <summary>
/// Metadata for a lifecycle hook
/// </summary>
public sealed class HookMetadata
{
    public required string Name { get; init; }
    public required HookLevel Level { get; init; }
    public int Order { get; init; }
    
    /// <summary>
    /// AOT-safe hook invoker (null for reflection-based)
    /// </summary>
    public Func<object?, HookContext, Task>? Invoker { get; init; }
    
    /// <summary>
    /// For reflection scenarios
    /// </summary>
    public MethodInfo? MethodInfo { get; init; }
    public Type? DeclaringType { get; init; }
    public bool IsStatic { get; init; }
}

public enum HookLevel
{
    Assembly,
    Class,
    Test
}