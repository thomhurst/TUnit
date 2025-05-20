namespace TUnit.Assertions.SourceGenerator.Helpers;

public static class ForEachAppendBodyIndentedExtensions {
    public static GeneratorStringBuilder ForEachAppendBodyIndented(
        this GeneratorStringBuilder builder,
        IEnumerable<string> items
    ) => builder.ForEach(
        items,
        static (g, item) => g.AppendBodyIndented(item)
    );

    public static GeneratorStringBuilder ForEachAppendBodyIndented<TItem>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, string> itemFormatter
    ) => builder.ForEach(
        items,
        static (g, item, formatter) => g.AppendBodyIndented(formatter(item)),
        itemFormatter
    );

    public static GeneratorStringBuilder ForEachAppendBodyIndented<TItem, TArg>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, TArg, string> itemFormatter,
        TArg arg
    )
        => builder.ForEach(
            items,
            static (g, item, formatter, arg) => g.AppendBodyIndented(formatter(item, arg)),
            itemFormatter,
            arg
        );
}