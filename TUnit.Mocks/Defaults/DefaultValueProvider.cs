using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TUnit.Mocks;

/// <summary>
/// A built-in <see cref="IDefaultValueProvider"/> that returns smart defaults for common types.
/// <list type="bullet">
///   <item><description><c>string</c> returns <c>""</c></description></item>
///   <item><description><c>IEnumerable&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>, <c>ICollection&lt;T&gt;</c>,
///     <c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c> return empty <c>List&lt;T&gt;</c></description></item>
///   <item><description><c>Task</c> returns <c>Task.CompletedTask</c></description></item>
///   <item><description><c>Task&lt;T&gt;</c> returns <c>Task.FromResult(default(T))</c></description></item>
///   <item><description><c>ValueTask</c> returns <c>default(ValueTask)</c></description></item>
///   <item><description><c>ValueTask&lt;T&gt;</c> returns <c>new ValueTask&lt;T&gt;(default(T))</c></description></item>
///   <item><description>Value types return <c>default</c> (via <see cref="Activator.CreateInstance(Type)"/>)</description></item>
///   <item><description>Reference types return <c>null</c></description></item>
/// </list>
/// </summary>
public sealed class DefaultValueProvider : IDefaultValueProvider
{
    /// <summary>
    /// A shared singleton instance of the default value provider.
    /// </summary>
    public static DefaultValueProvider Instance { get; } = new();

    /// <inheritdoc />
    public bool CanProvide(Type type)
    {
        // This provider can supply a value for any type.
        return true;
    }

    /// <inheritdoc />
#pragma warning disable IL2067 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling
    public object? GetDefaultValue(Type type)
    {
        if (type == typeof(string))
        {
            return "";
        }

        if (type == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (type == typeof(ValueTask))
        {
            return default(ValueTask);
        }

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            // Task<T> -> Task.FromResult(default(T))
            if (genericDef == typeof(Task<>))
            {
                var innerDefault = GetDefaultForType(genericArgs[0]);
                // Use reflection to call Task.FromResult<T>(default(T))
                var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(genericArgs[0]);
                return fromResultMethod.Invoke(null, new[] { innerDefault });
            }

            // ValueTask<T> -> new ValueTask<T>(default(T))
            if (genericDef == typeof(ValueTask<>))
            {
                var innerDefault = GetDefaultForType(genericArgs[0]);
                return Activator.CreateInstance(type, innerDefault);
            }

            // Collection interfaces -> empty List<T>
            if (genericArgs.Length == 1)
            {
                var elementType = genericArgs[0];
                var listType = typeof(List<>).MakeGenericType(elementType);

                if (genericDef == typeof(IEnumerable<>)
                    || genericDef == typeof(IList<>)
                    || genericDef == typeof(ICollection<>)
                    || genericDef == typeof(IReadOnlyList<>)
                    || genericDef == typeof(IReadOnlyCollection<>))
                {
                    return Activator.CreateInstance(listType);
                }
            }
        }

        return GetDefaultForType(type);
    }
#pragma warning restore IL3050
#pragma warning restore IL2067

    private static object? GetDefaultForType(Type type)
    {
#pragma warning disable IL2067
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
#pragma warning restore IL2067

        return null;
    }
}
