// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------

namespace TUnit.Assertions.SourceGenerator.Helpers;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ForEachAppendLineExtensions {
    public static GeneratorStringBuilder ForEachAppendLine(
        this GeneratorStringBuilder builder,
        IEnumerable<string> items
    ) => builder.ForEach(
        items,
        static (g, item) => g.AppendLine(item)
    );

    public static GeneratorStringBuilder ForEachAppendLine<TItem>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, string> itemFormatter
    ) => builder.ForEach(
        items,
        static (g, item, formatter) => g.AppendLine(formatter(item)),
        itemFormatter
    );

    public static GeneratorStringBuilder ForEachAppendLine<TItem, TArg>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, TArg, string> itemFormatter,
        TArg arg
    ) => builder.ForEach(
        items,
        static (g, item, formatter, arg) => g.AppendLine(formatter(item, arg)),
        itemFormatter,
        arg
    );
}