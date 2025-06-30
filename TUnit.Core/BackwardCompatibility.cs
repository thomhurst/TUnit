using System;
using System.Collections.Generic;
using System.Linq;

namespace TUnit.Core;

/// <summary>
/// Backward compatibility shim for AotFriendlyTestDataSource
/// This class is deprecated and should not be used in new code
/// </summary>
[Obsolete("Use DynamicTestDataSource with FactoryKey instead")]
public sealed class AotFriendlyTestDataSource : TestDataSource
{
    public Func<object>? MethodInvoker { get; init; }
    public Type? SourceType { get; init; }
    public string? SourceMemberName { get; init; }
    private readonly bool _isShared;
    
    public override bool IsShared => _isShared;
    
    public AotFriendlyTestDataSource(bool isShared = false)
    {
        _isShared = isShared;
    }
    
    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        // Legacy implementation
        if (MethodInvoker != null)
        {
            var result = MethodInvoker();
            if (result is IEnumerable<object?[]> arrays)
            {
                return arrays.Select(arr => new Func<object?[]>(() => arr));
            }
        }
        return Enumerable.Empty<Func<object?[]>>();
    }
}

/// <summary>
/// Extension to DynamicTestDataSource for backward compatibility
/// </summary>
public static class DynamicTestDataSourceExtensions
{
    [Obsolete("Use FactoryKey instead")]
    public static Type? GetSourceType(this DynamicTestDataSource source)
    {
        // This is a compatibility shim - actual implementation would be in source generator
        return null;
    }
    
    [Obsolete("Use FactoryKey instead")]
    public static string? GetSourceMemberName(this DynamicTestDataSource source)
    {
        // This is a compatibility shim - actual implementation would be in source generator
        return null;
    }
    
    [Obsolete("Not needed in AOT mode")]
    public static object?[]? GetArguments(this DynamicTestDataSource source)
    {
        return null;
    }
}