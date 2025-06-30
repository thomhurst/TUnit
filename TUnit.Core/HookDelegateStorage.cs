using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TUnit.Core;

/// <summary>
/// Central storage for hook delegates used in AOT scenarios
/// </summary>
public static class HookDelegateStorage
{
    private static readonly Dictionary<string, Func<object?, HookContext, Task>> _hooks = new();
    private static readonly object _lock = new();
    
    /// <summary>
    /// Registers a hook delegate
    /// </summary>
    public static void RegisterHook(string key, Func<object?, HookContext, Task> hook)
    {
        lock (_lock)
        {
            _hooks[key] = hook;
        }
    }
    
    /// <summary>
    /// Gets a hook delegate by key
    /// </summary>
    public static Func<object?, HookContext, Task>? GetHook(string key)
    {
        lock (_lock)
        {
            return _hooks.TryGetValue(key, out var hook) ? hook : null;
        }
    }
    
    /// <summary>
    /// Clears all registered hooks (useful for testing)
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _hooks.Clear();
        }
    }
}