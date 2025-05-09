// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------

namespace TUnit.Assertions.SourceGenerator.Helpers;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ForEachExtensions {
    public static GeneratorStringBuilder ForEach<TItem>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Action<GeneratorStringBuilder, TItem> itemFormatter
    ) {
        if (items is ICollection<TItem> { Count: 0 }) return builder; // Skip iteration if no items

        foreach (TItem item in items) itemFormatter(builder, item);
        return builder;
    }

    public static GeneratorStringBuilder ForEach<TItem, TArg>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Action<GeneratorStringBuilder, TItem, TArg> itemFormatter,
        TArg arg
    ) {
        if (items is ICollection<TItem> { Count: 0 }) return builder; // Skip iteration if no items

        foreach (TItem item in items) itemFormatter(builder, item, arg);
        return builder;
    }

    public static GeneratorStringBuilder ForEach<TItem, TArg1, TArg2>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Action<GeneratorStringBuilder, TItem, TArg1, TArg2> itemFormatter,
        TArg1 arg1,
        TArg2 arg2
    ) {
        if (items is ICollection<TItem> { Count: 0 }) return builder; // Skip iteration if no items

        foreach (TItem item in items) itemFormatter(builder, item, arg1, arg2);
        return builder;
    }
}