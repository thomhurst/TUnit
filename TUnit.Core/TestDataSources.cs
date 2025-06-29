using System;
using System.Collections.Generic;
using System.Linq;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Base class for test data sources that provide data through factories
/// </summary>
public abstract class TestDataSource : IDataSource
{
    /// <summary>
    /// Gets factory functions that produce test data when invoked
    /// </summary>
    public abstract IEnumerable<Func<object?[]>> GetDataFactories();
    
    /// <summary>
    /// Indicates whether this data source is shared across tests
    /// </summary>
    public abstract bool IsShared { get; }
    
    /// <summary>
    /// Implementation of IDataSource interface
    /// </summary>
    public IEnumerable<Func<object?[]>> GenerateDataFactories(DataSourceContext context)
    {
        return GetDataFactories();
    }
}

/// <summary>
/// Static test data source for compile-time known values (e.g., from [Arguments] attribute)
/// </summary>
public sealed class StaticTestDataSource : TestDataSource
{
    private readonly object?[][] _data;
    
    public override bool IsShared => false;
    
    public StaticTestDataSource(params object?[][] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }
    
    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        // For static data, we create factories that return cloned arrays
        // to ensure test isolation even for reference types
        return _data.Select(args => new Func<object?[]>(() => CloneArguments(args)));
    }
    
    private static object?[] CloneArguments(object?[] args)
    {
        // Create a new array to ensure isolation
        var cloned = new object?[args.Length];
        Array.Copy(args, cloned, args.Length);
        return cloned;
    }
}

/// <summary>
/// Dynamic test data source that resolves data at runtime (e.g., from method or property)
/// </summary>
public sealed class DynamicTestDataSource : TestDataSource
{
    public required Type SourceType { get; init; }
    public required string SourceMemberName { get; init; }
    public object?[] Arguments { get; init; } = Array.Empty<object?>();
    private readonly bool _isShared;
    
    public override bool IsShared => _isShared;
    
    public DynamicTestDataSource(bool isShared = false)
    {
        _isShared = isShared;
    }
    
    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        // This will be resolved by the data source expander
        throw new NotImplementedException(
            "Dynamic data sources must be resolved through IDataSourceExpander");
    }
}

/// <summary>
/// AOT-friendly test data source that uses direct method invocation instead of reflection
/// </summary>
public sealed class AotFriendlyTestDataSource : TestDataSource
{
    public required Func<object> MethodInvoker { get; init; }
    public required Type SourceType { get; init; }
    public required string SourceMemberName { get; init; }
    private readonly bool _isShared;
    
    public override bool IsShared => _isShared;
    
    public AotFriendlyTestDataSource(bool isShared = false)
    {
        _isShared = isShared;
    }
    
    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        try
        {
            var result = MethodInvoker();
            var dataArrays = ConvertToObjectArrays(result);
            
            return dataArrays.Select(args => new Func<object?[]>(() => CloneArguments(args)));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to invoke data source method '{SourceMemberName}' on type '{SourceType.Name}'", ex);
        }
    }
    
    private static object?[][] ConvertToObjectArrays(object result)
    {
        return result switch
        {
            object?[][] arrays => arrays,
            IEnumerable<object?[]> enumerable => enumerable.ToArray(),
            IEnumerable<object> objects => objects.Select(obj => new object?[] { obj }).ToArray(),
            System.Collections.IEnumerable enumerable => enumerable.Cast<object?>().Select(obj => new object?[] { obj }).ToArray(),
            _ => new[] { new object?[] { result } }
        };
    }
    
    private static object?[] CloneArguments(object?[] args)
    {
        // Create a new array to ensure isolation
        var cloned = new object?[args.Length];
        Array.Copy(args, cloned, args.Length);
        return cloned;
    }
}

/// <summary>
/// Property data source that provides a single value for property injection
/// </summary>
public sealed class PropertyDataSource
{
    public required string PropertyName { get; init; }
    public required Type PropertyType { get; init; }
    public required TestDataSource DataSource { get; init; }
}