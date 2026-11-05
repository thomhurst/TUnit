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
    /// <summary>Data member declared as a single (non-enumerable, non-task) value.</summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromValue(Func<object?> invoke)
        => FromValueCore(invoke, tupleRowBinding: TupleRowBinding.Spread);

    /// <summary>
    /// <see cref="FromValue(Func{object?})"/> that keeps a tuple row whole when it binds to the first tuple parameter,
    /// as the attribute's reflection path does.
    /// </summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromValue(DataGeneratorMetadata dataGeneratorMetadata, Func<object?> invoke)
        => FromValueCore(invoke, GetTupleRowBinding(dataGeneratorMetadata));

    /// <summary>Data member declared as a synchronous enumerable.</summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromEnumerable(Func<object?> invoke)
        => FromEnumerableCore(invoke, tupleRowBinding: TupleRowBinding.Spread);

    /// <summary>
    /// <see cref="FromEnumerable(Func{object?})"/> that keeps a tuple row whole when it binds to the first tuple parameter,
    /// as the attribute's reflection path does.
    /// </summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromEnumerable(DataGeneratorMetadata dataGeneratorMetadata, Func<object?> invoke)
        => FromEnumerableCore(invoke, GetTupleRowBinding(dataGeneratorMetadata));

    /// <summary>Data member declared as <c>Task&lt;T&gt;</c>.</summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromTask<T>(Func<Task<T>> invoke)
        => FromTaskCore(invoke, tupleRowBinding: TupleRowBinding.Spread);

    /// <summary>
    /// <see cref="FromTask{T}(Func{Task{T}})"/> that keeps a tuple row whole when it binds to the first tuple parameter,
    /// as the attribute's reflection path does.
    /// </summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromTask<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<Task<T>> invoke)
        => FromTaskCore(invoke, GetTupleRowBinding(dataGeneratorMetadata));

    /// <summary>Data member declared as <c>ValueTask&lt;T&gt;</c>.</summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromValueTask<T>(Func<ValueTask<T>> invoke)
        => FromValueTaskCore(invoke, tupleRowBinding: TupleRowBinding.Spread);

    /// <summary>
    /// <see cref="FromValueTask{T}(Func{ValueTask{T}})"/> that keeps a tuple row whole when it binds to the first tuple parameter,
    /// as the attribute's reflection path does.
    /// </summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromValueTask<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<ValueTask<T>> invoke)
        => FromValueTaskCore(invoke, GetTupleRowBinding(dataGeneratorMetadata));

    /// <summary>Data member declared as <c>IAsyncEnumerable&lt;T&gt;</c>.</summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromAsyncEnumerable<T>(Func<IAsyncEnumerable<T>> invoke)
        => FromAsyncEnumerableCore(invoke, tupleRowBinding: TupleRowBinding.Spread);

    /// <summary>
    /// <see cref="FromAsyncEnumerable{T}(Func{IAsyncEnumerable{T}})"/> that keeps a tuple row whole when it binds to the
    /// first tuple parameter, as the attribute's reflection path does.
    /// </summary>
    public static IAsyncEnumerable<Func<Task<object?[]?>>> FromAsyncEnumerable<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<IAsyncEnumerable<T>> invoke)
        => FromAsyncEnumerableCore(invoke, GetTupleRowBinding(dataGeneratorMetadata));

#pragma warning disable CS1998 // sync-only async iterators: deferred invocation, no await by design
    private static async IAsyncEnumerable<Func<Task<object?[]?>>> FromValueCore(Func<object?> invoke, TupleRowBinding tupleRowBinding)
    {
        var result = invoke();

        yield return () => Task.FromResult<object?[]?>(ToRow(result, tupleRowBinding));
    }

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> FromEnumerableCore(Func<object?> invoke, TupleRowBinding tupleRowBinding)
    {
        var result = invoke();

        if (result is null)
        {
            yield break;
        }

        foreach (var row in RowsFromValue(result, tupleRowBinding))
        {
            yield return row;
        }
    }
#pragma warning restore CS1998

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> FromTaskCore<T>(Func<Task<T>> invoke, TupleRowBinding tupleRowBinding)
    {
        var result = invoke();

        // A null task is a source with no rows, as the attribute's reflection path reads it, so SkipIfEmpty skips the test.
        if (result is null)
        {
            yield break;
        }

        var taskResult = await result;
        if (taskResult is null && IsDeclaredCollection<T>())
        {
            yield break;
        }

        foreach (var row in RowsFromValue(taskResult, tupleRowBinding))
        {
            yield return row;
        }
    }

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> FromValueTaskCore<T>(Func<ValueTask<T>> invoke, TupleRowBinding tupleRowBinding)
    {
        var taskResult = await invoke();
        if (taskResult is null && IsDeclaredCollection<T>())
        {
            yield break;
        }

        foreach (var row in RowsFromValue(taskResult, tupleRowBinding))
        {
            yield return row;
        }
    }

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> FromAsyncEnumerableCore<T>(Func<IAsyncEnumerable<T>> invoke, TupleRowBinding tupleRowBinding)
    {
        var result = invoke();

        // A null source has no rows, as the attribute's reflection path reads it, so SkipIfEmpty skips the test.
        if (result is null)
        {
            yield break;
        }

        await foreach (var item in result)
        {
            yield return () => Task.FromResult<object?[]?>(ToRow(item, tupleRowBinding));
        }
    }

#pragma warning disable CS1998 // sync-only async iterators: deferred invocation, no await by design
    /// <summary>Instance data member declared as a single (non-enumerable, non-task) value.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceValue(DataGeneratorMetadata dataGeneratorMetadata, Func<object, object?> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));
        var tupleRowBinding = GetTupleRowBinding(dataGeneratorMetadata);

        yield return () => Task.FromResult<object?[]?>(ToRow(result, tupleRowBinding));
    }

    /// <summary>Instance data member declared as a synchronous enumerable.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceEnumerable(DataGeneratorMetadata dataGeneratorMetadata, Func<object, object?> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));
        var tupleRowBinding = GetTupleRowBinding(dataGeneratorMetadata);

        if (result is null)
        {
            yield break;
        }

        foreach (var row in RowsFromValue(result, tupleRowBinding))
        {
            yield return row;
        }
    }
#pragma warning restore CS1998

    /// <summary>Instance data member declared as <c>Task&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceTask<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<object, Task<T>> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));
        var tupleRowBinding = GetTupleRowBinding(dataGeneratorMetadata);

        // A null task is a source with no rows, as the attribute's reflection path reads it, so SkipIfEmpty skips the test.
        if (result is null)
        {
            yield break;
        }

        var taskResult = await result;
        if (taskResult is null && IsDeclaredCollection<T>())
        {
            yield break;
        }

        foreach (var row in RowsFromValue(taskResult, tupleRowBinding))
        {
            yield return row;
        }
    }

    /// <summary>Instance data member declared as <c>ValueTask&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceValueTask<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<object, ValueTask<T>> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));
        var tupleRowBinding = GetTupleRowBinding(dataGeneratorMetadata);

        var taskResult = await result;
        if (taskResult is null && IsDeclaredCollection<T>())
        {
            yield break;
        }

        foreach (var row in RowsFromValue(taskResult, tupleRowBinding))
        {
            yield return row;
        }
    }

    /// <summary>Instance data member declared as <c>IAsyncEnumerable&lt;T&gt;</c>.</summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> FromInstanceAsyncEnumerable<T>(DataGeneratorMetadata dataGeneratorMetadata, Func<object, IAsyncEnumerable<T>> invoke)
    {
        var result = invoke(GetRequiredInstance(dataGeneratorMetadata));
        var tupleRowBinding = GetTupleRowBinding(dataGeneratorMetadata);

        // A null source has no rows, as the attribute's reflection path reads it, so SkipIfEmpty skips the test.
        if (result is null)
        {
            yield break;
        }

        await foreach (var item in result)
        {
            yield return () => Task.FromResult<object?[]?>(ToRow(item, tupleRowBinding));
        }
    }

    /// <summary>
    /// A data source field, or a private or protected property that the generated code reads by reflection, whose
    /// declared type does not say whether it holds one row or many, such as <c>object</c>. The value's runtime type decides,
    /// as it does when <see cref="MethodDataSourceAttribute"/> has no factory and reads the member itself.
    /// A null value is one row holding null, as <see cref="FromValue(Func{object?})"/> gives.
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

    // Splits a data member's value into rows the way FromEnumerable always has: an enumerable other than a string gives
    // one row per item, anything else is one row.
    private static IEnumerable<Func<Task<object?[]?>>> RowsFromValue(object? value, TupleRowBinding tupleRowBinding)
    {
        if (value is IEnumerable enumerable && !(value is string))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(ToRow(item, tupleRowBinding));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(ToRow(value, tupleRowBinding));
        }
    }

    // Preserve tuple-valued properties and distinguish null rows from tuples holding null.
    // Method arguments with struct initializers need flattened boxes for reconstruction after initialization.
    private readonly record struct TupleRowBinding(bool Preserve, Type? MatchingType = null, bool AllowMutableElements = false)
    {
        public static readonly TupleRowBinding Spread = new(false);
        public static readonly TupleRowBinding PreserveWhole = new(true, AllowMutableElements: true);
        public static readonly TupleRowBinding PreserveUnlessInitializer = new(true);
    }

    private static TupleRowBinding GetTupleRowBinding(DataGeneratorMetadata dataGeneratorMetadata)
        => dataGeneratorMetadata.MembersToGenerate switch
        {
            [PropertyMetadata property] when TupleHelper.IsTupleType(property.Type) => TupleRowBinding.PreserveWhole,
            [ParameterMetadata parameter] when TupleHelper.IsTupleType(parameter.Type) => TupleRowBinding.PreserveUnlessInitializer,
            // Match the row type so ((pair), extra) still spreads into two arguments.
            [ParameterMetadata first, ..] when TupleHelper.IsTupleType(first.Type) => new(true, first.Type),
            _ => TupleRowBinding.Spread
        };

    private static object?[] ToRow(object? item, TupleRowBinding tupleRowBinding)
    {
        if (tupleRowBinding.Preserve)
        {
            item = DataSourceHelpers.InvokeIfFunc(item);
            if (DataSourceHelpers.IsTuple(item)
                && (tupleRowBinding.MatchingType is null || tupleRowBinding.MatchingType.IsInstanceOfType(item)))
            {
                if (!tupleRowBinding.AllowMutableElements && DataSourceHelpers.RequiresFlattenedTupleBinding(item!))
                {
                    return tupleRowBinding.MatchingType is null
                        ? DataSourceHelpers.RetainTupleElements(item)
                        : [new TupleArgument(item!.GetType(), DataSourceHelpers.RetainTupleElements(item))];
                }

                return [item];
            }
        }

        return DataSourceHelpers.ToObjectArray(item);
    }

    private static bool IsDeclaredCollection<T>() =>
        typeof(T) != typeof(string) && typeof(IEnumerable).IsAssignableFrom(typeof(T));

    private static object GetRequiredInstance(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return dataGeneratorMetadata.TestClassInstance
            ?? throw new InvalidOperationException("Instance method data source requires TestClassInstance. This should have been provided by the engine.");
    }
}
