using System;
using System.Collections.Generic;
using System.Linq;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Base class for test data sources that provide data through factories
/// </summary>
public abstract class TestDataSource
{
    /// <summary>
    /// Gets factory functions that produce test data when invoked
    /// </summary>
    public abstract IEnumerable<Func<object?[]>> GetDataFactories();
    
    /// <summary>
    /// Indicates whether this data source is shared across tests
    /// </summary>
    public virtual bool IsShared => false;
}

/// <summary>
/// Static test data source for compile-time known values (e.g., from [Arguments] attribute)
/// </summary>
public sealed class StaticTestDataSource : TestDataSource
{
    private readonly object?[][] _data;
    
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
    public override bool IsShared { get; init; }
    
    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        // This will be resolved by the data source expander
        throw new NotImplementedException(
            "Dynamic data sources must be resolved through IDataSourceExpander");
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