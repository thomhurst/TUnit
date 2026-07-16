using System.Collections.Generic;
using System.Text;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Single source of truth for the collection-shape → assertion-source → seed map, shared by
/// <see cref="CollectionShapeAssertionGenerator"/> (the full wrapper surface) and
/// <see cref="CollectionShapeOverloadGenerator"/> (the per-shape <c>Satisfies</c>/<c>Count</c> overloads).
/// </summary>
internal static class CollectionShapeRegistry
{
    /// <summary>
    /// How a shape's assertion source is constructed from an <c>AssertionContext&lt;Shape&gt;</c>. Adding a row
    /// to <see cref="Rows"/> requires the matching seed member on the source class:
    /// <list type="bullet">
    /// <item><see cref="Ctor"/> — an internal ctor <c>(AssertionContext&lt;Shape&gt;)</c>.</item>
    /// <item><see cref="FromContext"/> — an internal static <c>FromContext(AssertionContext&lt;Shape&gt;)</c> factory.</item>
    /// <item><see cref="UpcastCtor"/> — a concrete shape (e.g. <c>List&lt;T&gt;</c>) whose ctor takes the
    /// interface context, so the context is identity-upcast (<c>UpcastTargetFormat</c>) first, preserving pending pre-work.</item>
    /// </list>
    /// </summary>
    internal enum SeedKind { Ctor, FromContext, UpcastCtor }

    internal sealed record ShapeRow(
        string SourceMetadataName,
        string ShapeFormat,
        SeedKind Seed,
        string? UpcastTargetFormat = null,
        string? NetGuard = null);

    // The shape -> source -> seed table. Only this small list is hardcoded; each source's method surface is
    // reflected. Mirrors the collection-shaped Assert.That overloads in Extensions/Assert.cs (and the shape set
    // the hand-written fan-out covered). Non-collection value-shapes that have That() overloads — Memory<T>,
    // ReadOnlyMemory<T>, IAsyncEnumerable<T> — are intentionally excluded: they are not collection-value wrappers.
    internal static readonly ShapeRow[] Rows =
    {
        new("TUnit.Assertions.Sources.CollectionAssertion`1", "global::System.Collections.Generic.IEnumerable<{0}>", SeedKind.Ctor),
        new("TUnit.Assertions.Sources.ReadOnlyListAssertion`1", "global::System.Collections.Generic.IReadOnlyList<{0}>", SeedKind.Ctor),
        new("TUnit.Assertions.Sources.ListAssertion`1", "global::System.Collections.Generic.IList<{0}>", SeedKind.Ctor),
        new("TUnit.Assertions.Sources.ListAssertion`1", "global::System.Collections.Generic.List<{0}>", SeedKind.UpcastCtor, "global::System.Collections.Generic.IList<{0}>"),
        new("TUnit.Assertions.Sources.ArrayAssertion`1", "{0}[]", SeedKind.Ctor),
        new("TUnit.Assertions.Sources.SetAssertion`1", "global::System.Collections.Generic.ISet<{0}>", SeedKind.FromContext),
        new("TUnit.Assertions.Sources.HashSetAssertion`1", "global::System.Collections.Generic.HashSet<{0}>", SeedKind.FromContext),
        new("TUnit.Assertions.Sources.ReadOnlySetAssertion`1", "global::System.Collections.Generic.IReadOnlySet<{0}>", SeedKind.FromContext, NetGuard: "NET5_0_OR_GREATER"),
        new("TUnit.Assertions.Sources.DictionaryAssertion`2", "global::System.Collections.Generic.IReadOnlyDictionary<{0}, {1}>", SeedKind.Ctor),
        new("TUnit.Assertions.Sources.MutableDictionaryAssertion`2", "global::System.Collections.Generic.IDictionary<{0}, {1}>", SeedKind.Ctor),
        new("TUnit.Assertions.Sources.MutableDictionaryAssertion`2", "global::System.Collections.Generic.Dictionary<{0}, {1}>", SeedKind.UpcastCtor, "global::System.Collections.Generic.IDictionary<{0}, {1}>"),
    };

    // Precomputed per-shape strings for the single-template overload emitters (Satisfies/Count). Names: TInner
    // for single-item shapes, (TKey, TValue) for dictionaries — matching the existing hand-written public
    // signatures so the migration is API-net-zero.
    internal sealed record ItemShape(string Shape, string SourceClosed, string Names, string DictConstraint, string? NetGuard);

    internal static readonly ItemShape[] ItemShapes = BuildItemShapes();

    private static ItemShape[] BuildItemShapes()
    {
        var result = new ItemShape[Rows.Length];
        for (var i = 0; i < Rows.Length; i++)
        {
            var row = Rows[i];
            var tick = row.SourceMetadataName.IndexOf('`');
            // The metadata name ends in `1 / `2 — read the arity digit directly (no parse / culture / throw).
            var arity = row.SourceMetadataName[tick + 1] - '0';
            var sourceFq = "global::" + row.SourceMetadataName.Substring(0, tick);
            var nameArgs = arity == 2 ? new object[] { "TKey", "TValue" } : new object[] { "TInner" };
            var names = string.Join(", ", nameArgs);
            result[i] = new ItemShape(
                Shape: string.Format(row.ShapeFormat, nameArgs),
                SourceClosed: $"{sourceFq}<{names}>",
                Names: names,
                DictConstraint: arity == 2 ? "\n        where TKey : notnull" : "",
                NetGuard: row.NetGuard);
        }

        return result;
    }

    // Common preamble (header + namespace, optionally gated to non-netstandard2.0) for a generated extensions file.
    internal static StringBuilder StartGeneratedFile(bool netStandardOnly = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable enable");
        if (netStandardOnly)
        {
            sb.AppendLine("#if !NETSTANDARD2_0");
        }
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Assertions.Extensions;");
        sb.AppendLine();
        return sb;
    }
}
