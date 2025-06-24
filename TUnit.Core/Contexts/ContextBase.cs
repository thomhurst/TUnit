using System;
using System.Collections.Generic;

namespace TUnit.Core.Contexts;

/// <summary>
/// Base class for all context types in TUnit
/// </summary>
public abstract class ContextBase
{
    /// <summary>
    /// Dictionary for storing arbitrary data that can be shared across different phases
    /// </summary>
    public Dictionary<string, object?> Items { get; } = new();
    
    /// <summary>
    /// Lock object for thread-safe operations
    /// </summary>
    public object Lock { get; } = new object();
    
    protected ContextBase()
    {
    }
}