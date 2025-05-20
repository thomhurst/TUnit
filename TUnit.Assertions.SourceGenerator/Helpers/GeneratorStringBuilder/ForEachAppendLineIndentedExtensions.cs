namespace TUnit.Assertions.SourceGenerator.Helpers;

public static class ForEachAppendLineIndentedExtensions {
    public static GeneratorStringBuilder ForEachAppendLineIndented(
        this GeneratorStringBuilder builder,
        IEnumerable<string> items
    ) => builder.ForEach(
        items,
        static (g, item) => g.AppendLineIndented(item)
    );

    public static GeneratorStringBuilder ForEachAppendLineIndented<TItem>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, string> itemFormatter
    ) => builder.ForEach(
        items,
        static (g, item, formatter) => g.AppendLineIndented(formatter(item)),
        itemFormatter
    );

    public static GeneratorStringBuilder ForEachAppendLineIndented<TItem, TArg>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, TArg, string> itemFormatter,
        TArg arg
    )
        => builder.ForEach(
            items,
            static (g, item, formatter, arg) => g.AppendLineIndented(formatter(item, arg)),
            itemFormatter,
            arg
        );
}