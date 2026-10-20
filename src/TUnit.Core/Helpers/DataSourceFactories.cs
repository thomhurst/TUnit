// Sync helper variants (FromValue/FromEnumerable and their instance twins) are async iterators
// with no await — intentional, so the data-source invocation stays deferred to first enumeration
// with semantics identical to the previously generated per-call-site iterators. CS1998 is
// suppressed only around those methods (not file-scope) so a genuine missing await elsewhere
// still surfaces.

using System.Collections;
using System.ComponentModel;

namespace TUnit.Core.Helpers;

/// <summary>
/// Shared factory helpers invoked by source-generated data source attributes.
/// </summary>
/// <remarks>
/// <para>
/// Each helper replaces a per-call-site compiler-generated async iterator that the source
/// generator previously emitted inline for every <c>[MethodDataSource]</c>. Centralizing the
/// iterator bodies here means they are JIT-compiled once instead of once per data source,
/// shrinking generated IL and discovery-time tier-0 JIT cost. Generated code passes only the
/// per-test-unique invocation as a static lambda.
/// </para>
/// <para>
/// The helper variants mirror the generator's compile-time dispatch on the data member's
/// declared return type: <c>IAsyncEnumerable&lt;T&gt;</c>, <c>Task&lt;T&gt;</c>/<c>ValueTask&lt;T&gt;</c>,
/// <c>IEnumerable</c>, or a single value. The single-value variants intentionally skip the
/// runtime <see cref="IEnumerable"/> check so an <c>object</c>-declared return holding a
/// collection is still treated as one value — matching the previous generated code exactly.
/// </para>
/// </remarks>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class DataSourceFactories
{
#pragma warning disable CS1998 // sync-only async iterators: deferred invocation, no await by design
    /// <summary>Data member declared as a single (non-enumerable, non-task) value.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromValue(Func<object?> invoke)
    {
        var result = invoke();

        yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(result));
    }

    /// <summary>Data member declared as a synchronous enumerable.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromEnumerable(Func<object?> invoke)
    {
        var result = invoke();

        if (result is IEnumerable enumerable && !(result is string))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(item));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(result));
        }
    }
#pragma warning restore CS1998

    /// <summary>Data member declared as <c>Task&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromTask<T>(Func<Task<T>> invoke)
    {
        var result = invoke();

        var taskResult = await result;
        if (taskResult is IEnumerable enumerable && !(taskResult is string))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(item));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(taskResult));
        }
    }

    /// <summary>Data member declared as <c>ValueTask&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromValueTask<T>(Func<ValueTask<T>> invoke)
    {
        var result = invoke();

        var taskResult = await result;
        if (taskResult is IEnumerable enumerable && !(taskResult is string))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(item));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(taskResult));
        }
    }

    /// <summary>Data member declared as <c>IAsyncEnumerable&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromAsyncEnumerable<T>(Func<IAsyncEnumerable<T>> invoke)
    {
        var result = invoke();

        await foreach (var item in result)
        {
            yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(item));
        }
    }

#pragma warning disable CS1998 // sync-only async iterators: deferred invocation, no await by design
    /// <summary>Instance data member declared as a single (non-enumerable, non-task) value.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceValue(DataGeneratorMetadata dataGeneratorMetadata, Func<object, object?> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));

        yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(result));
    }

    /// <summary>Instance data member declared as a synchronous enumerable.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceEnumerable(DataGeneratorMetadata dataGeneratorMetadata, Func<object, object?> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));

        if (result is IEnumerable enumerable && !(result is string))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(item));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(result));
        }
    }
#pragma warning restore CS1998

    /// <summary>Instance data member declared as <c>Task&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceTask<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<object, Task<T>> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));

        var taskResult = await result;
        if (taskResult is IEnumerable enumerable && !(taskResult is string))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(item));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(taskResult));
        }
    }

    /// <summary>Instance data member declared as <c>ValueTask&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceValueTask<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<object, ValueTask<T>> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));

        var taskResult = await result;
        if (taskResult is IEnumerable enumerable && !(taskResult is string))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(item));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(taskResult));
        }
    }

    /// <summary>Instance data member declared as <c>IAsyncEnumerable&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceAsyncEnumerable<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<object, IAsyncEnumerable<T>> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));

        await foreach (var item in result)
        {
            yield return () => Task.FromResult<object?[]?>(DataSourceHelpers.ToObjectArray(item));
        }
    }

    /// <summary>
    /// A data source field, or a private or protected property that the generated code reads by reflection, whose
    /// declared type does not say whether it holds one row or many, such as <c>object</c>. The value's runtime type decides,
    /// as it does when <see cref="MethodDataSourceAttribute"/> has no factory and reads the member itself.
    /// A null value is one row holding null, as <see cref="FromValue"/> gives.
    /// </summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromRuntimeValue(DataGeneratorMetadata dataGeneratorMetadata, Func<object?> invoke)
    {
        if (invoke() is not { } result)
        {
            yield return NullValueRow;
            yield break;
        }

        await foreach (var row in MethodDataSourceAttribute.GetRowsFromRuntimeValueAsync(result, dataGeneratorMetadata))
        {
            yield return row;
        }
    }

    /// <summary>Instance twin of <see cref="FromRuntimeValue"/>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceRuntimeValue(DataGeneratorMetadata dataGeneratorMetadata, Func<object, object?> invoke)
    {
        if (invoke(GetRequiredInstance(dataGeneratorMetadata)) is not { } result)
        {
            yield return NullValueRow;
            yield break;
        }

        await foreach (var row in MethodDataSourceAttribute.GetRowsFromRuntimeValueAsync(result, dataGeneratorMetadata))
        {
            yield return row;
        }
    }

    // A null value is one row holding null, as FromValue gives for a member whose declared type picks it. Yielding
    // nothing instead would become an empty row, so the test would fail for a missing argument.
    private static readonly Func<Task<object?[]?>> NullValueRow = static () => Task.FromResult<object?[]?>([null]);

    private static object GetRequiredInstance(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return dataGeneratorMetadata.TestClassInstance
            ?? throw new InvalidOperationException("Instance method data source requires TestClassInstance. This should have been provided by the engine.");
    }
}
