namespace TUnit.Assertions.SourceGenerator.Helpers;

public static class ForEachAppendBodyExtensions {
    public static GeneratorStringBuilder ForEachAppendBody(
        this GeneratorStringBuilder builder,
        IEnumerable<string> items
    ) => builder.ForEach(
        items,
        static (g, item) => g.AppendBody(item)
    );

    public static GeneratorStringBuilder ForEachAppendBody<TItem>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, string> itemFormatter
    ) => builder.ForEach(
        items,
        static (g, item, formatter) => g.AppendBody(formatter(item)),
        itemFormatter
    );

    public static GeneratorStringBuilder ForEachAppendBody<TItem, TArg>(
        this GeneratorStringBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, TArg, string> itemFormatter,
        TArg arg
    ) => builder.ForEach(
        items,
        static (g, item, formatter, arg) => g.AppendBody(formatter(item, arg)),
        itemFormatter,
        arg
    );
}