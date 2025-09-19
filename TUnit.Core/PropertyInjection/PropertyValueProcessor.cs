using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core.Services;
using TUnit.Core.Tracking;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Processes property values during injection.
/// Handles value resolution, tracking, and recursive injection.
/// </summary>
internal static class PropertyValueProcessor
{
    /// <summary>
    /// Resolves Func<T> values by invoking them without using reflection (AOT-safe).
    /// </summary>
    public static ValueTask<object?> ResolveTestDataValueAsync(Type type, object? value)
    {
        if (value == null)
        {
            return new ValueTask<object?>(result: null);
        }

        if (value is Delegate del)
        {
            // Use DynamicInvoke which is AOT-safe for parameterless delegates
            var result = del.DynamicInvoke();
            return new ValueTask<object?>(result);
        }

        return new ValueTask<object?>(value);
    }
}