using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.SourceGenerator.Models;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Generates the collection-shape "fan-out" forwarding overloads that were previously hand-written once per
/// (collection shape × method). Emitter A handles the full-surface <c>.Value</c>-style drill-in: a generic
/// arity-1 assertion source marked with <c>[GenerateCollectionShapeDrillIns]</c> gets, for every collection
/// shape, one forwarding extension per public method of that shape's assertion source — reflected from the
/// real <see cref="IMethodSymbol"/> so per-shape signature differences are always correct. See issue #6185.
/// </summary>
[Generator]
public sealed class CollectionShapeFanOutGenerator : IIncrementalGenerator
{
    private const string DrillInAttribute = "TUnit.Assertions.Attributes.GenerateCollectionShapeDrillInsAttribute";
    private const string ItemSourceAttribute = "TUnit.Assertions.Attributes.GenerateCollectionShapeItemSourceOverloadsAttribute";
    private const string CountAttribute = "TUnit.Assertions.Attributes.GenerateCollectionShapeCountOverloadsAttribute";

    private static readonly SymbolDisplayFormat Fq = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    // Names already present on IAssertionSource<T> / ValueAssertion<T> / Assertion<T> / object, or that are
    // pure infrastructure — forwarding them would be redundant or ambiguous.
    private static readonly HashSet<string> ExcludedNames = new()
    {
        "IsNull", "IsNotNull", "IsTypeOf", "IsNotTypeOf",
        "IsAssignableTo", "IsNotAssignableTo", "IsAssignableFrom", "IsNotAssignableFrom",
        "GetExpectation", "CheckAsync", "AssertAsync", "GetAwaiter", "Because",
        "Equals", "GetHashCode", "ToString", "GetType",
    };

    private enum SeedKind { Ctor, FromContext, UpcastCtor }

    private sealed record ShapeRow(
        string SourceMetadataName,
        string ShapeFormat,
        SeedKind Seed,
        string? UpcastTargetFormat = null,
        string? NetGuard = null);

    // The shape -> source -> seed table. Only this small list is hardcoded; each source's method surface is
    // reflected. Mirrors the collection-shaped Assert.That overloads in Extensions/Assert.cs (and the shape set
    // the hand-written fan-out covered). Non-collection value-shapes that have That() overloads — Memory<T>,
    // ReadOnlyMemory<T>, IAsyncEnumerable<T> — are intentionally excluded: they are not collection drill-in targets.
    private static readonly ShapeRow[] Rows =
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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var drillIns = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DrillInAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => BuildDrillInModel(ctx, ct))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .WithTrackingName("CollectionShapeDrillInTargets");

        context.RegisterSourceOutput(drillIns, static (spc, model) => EmitDrillIn(spc, model));

        // Emitter B — per-shape Satisfies overloads (replaces CollectionItemSatisfiesExtensions.cs).
        var itemSourceTrigger = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ItemSourceAttribute,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (_, _) => true)
            .WithTrackingName("CollectionShapeItemSourceTrigger");
        context.RegisterSourceOutput(itemSourceTrigger, static (spc, _) => EmitItemSourceOverloads(spc));

        // Emitter C — per-shape Count(itemAssertion) overloads (replaces the #5707 block in AssertionExtensions.cs).
        var countTrigger = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                CountAttribute,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (_, _) => true)
            .WithTrackingName("CollectionShapeCountTrigger");
        context.RegisterSourceOutput(countTrigger, static (spc, _) => EmitCountOverloads(spc));
    }

    // Shapes for the single-template Emitters B and C, precomputed once from the static registry (the transform
    // for these emitters carries no per-compilation data). Names: TInner for single-item shapes, (TKey, TValue)
    // for dictionaries — matching the existing hand-written public signatures so the migration is API-net-zero.
    private static readonly ItemShape[] ItemShapes = BuildItemShapes();

    private sealed record ItemShape(string Shape, string SourceClosed, string Names, string DictConstraint, string? NetGuard);

    private static ItemShape[] BuildItemShapes()
    {
        var result = new ItemShape[Rows.Length];
        for (var i = 0; i < Rows.Length; i++)
        {
            var row = Rows[i];
            var tick = row.SourceMetadataName.IndexOf('`');
            var arity = int.Parse(row.SourceMetadataName.Substring(tick + 1));
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
    private static StringBuilder StartGeneratedFile(bool netStandardOnly = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable enable");
        if (netStandardOnly) sb.AppendLine("#if !NETSTANDARD2_0");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Assertions.Extensions;");
        sb.AppendLine();
        return sb;
    }

    private static void EmitItemSourceOverloads(SourceProductionContext context)
    {
        var sb = StartGeneratedFile(netStandardOnly: true);
        sb.AppendLine("/// <summary>Generated per-collection-shape <c>Satisfies</c> overloads (issue #6185).</summary>");
        sb.AppendLine("public static class CollectionItemSatisfiesExtensions");
        sb.AppendLine("{");

        foreach (var s in ItemShapes)
        {
            if (s.NetGuard is not null) sb.AppendLine($"#if {s.NetGuard}");
            sb.AppendLine($"    public static TResult Satisfies<{s.Names}, TResult>(");
            sb.AppendLine($"        this global::TUnit.Assertions.Core.IItemSatisfiesSource<{s.Shape}, TResult> source,");
            sb.AppendLine($"        global::System.Func<{s.SourceClosed}, global::TUnit.Assertions.Core.IAssertion?> assertion,");
            sb.Append("        [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"assertion\")] string? expression = null)");
            sb.Append(s.DictConstraint);
            sb.AppendLine();
            sb.AppendLine($"        => source.Satisfies<{s.SourceClosed}>(assertion, expression);");
            if (s.NetGuard is not null) sb.AppendLine("#endif");
        }

        sb.AppendLine("}");
        sb.AppendLine("#endif");
        context.AddSource("CollectionItemSatisfiesExtensions.g.cs", sb.ToString());
    }

    private static void EmitCountOverloads(SourceProductionContext context)
    {
        var sb = StartGeneratedFile();
        sb.AppendLine("public static partial class AssertionExtensions");
        sb.AppendLine("{");

        foreach (var s in ItemShapes)
        {
            if (s.NetGuard is not null) sb.AppendLine($"#if {s.NetGuard}");
            sb.AppendLine($"    public static global::TUnit.Assertions.Conditions.CollectionCountSource<TCollection, {s.Shape}> Count<TCollection, {s.Names}>(");
            sb.AppendLine($"        this global::TUnit.Assertions.Sources.CollectionAssertionBase<TCollection, {s.Shape}> source,");
            sb.AppendLine($"        global::System.Func<{s.SourceClosed}, global::TUnit.Assertions.Core.IAssertion?> itemAssertion,");
            sb.AppendLine("        [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"itemAssertion\")] string? expression = null)");
            sb.Append($"        where TCollection : global::System.Collections.Generic.IEnumerable<{s.Shape}>");
            sb.Append(s.DictConstraint);
            sb.AppendLine();
            sb.AppendLine($"        => CountSpecialised<TCollection, {s.Shape}>(source, (item, index) => itemAssertion(new {s.SourceClosed}(item, $\"item[{{index}}]\")), expression);");
            if (s.NetGuard is not null) sb.AppendLine("#endif");
        }

        sb.AppendLine("}");
        context.AddSource("AssertionExtensions.CollectionShapeCount.g.cs", sb.ToString());
    }

    // ---------------------------------------------------------------------------------------------------
    // Model (fully equatable — no ISymbol stored)
    // ---------------------------------------------------------------------------------------------------

    private sealed record DrillInModel(
        string WrapperName,
        string WrapperOpenType,
        ImmutableEquatableArray<ShapeModel> Shapes);

    private sealed record ShapeModel(
        string ReceiverType,
        string? NetGuard,
        ImmutableEquatableArray<ForwardMethod> Methods);

    private sealed record ForwardMethod(
        string Signature,   // everything from "public static" up to the param list close paren + constraints
        string Body);       // "=> <seed>.Name<...>(args);"

    // ---------------------------------------------------------------------------------------------------
    // Transform
    // ---------------------------------------------------------------------------------------------------

    private static DrillInModel? BuildDrillInModel(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol target || target.TypeParameters.Length != 1)
        {
            return null;
        }

        var compilation = context.SemanticModel.Compilation;
        var wrapperOpen = $"global::{target.ContainingNamespace.ToDisplayString()}.{target.Name}";

        var shapes = new List<ShapeModel>();
        foreach (var row in Rows)
        {
            ct.ThrowIfCancellationRequested();

            var source = compilation.GetTypeByMetadataName(row.SourceMetadataName);
            if (source is null)
            {
                continue;
            }

            var typeParamNames = source.TypeParameters.Select(t => (object)t.Name).ToArray();
            var shapeType = string.Format(row.ShapeFormat, typeParamNames);
            var receiver = $"{wrapperOpen}<{shapeType}>";
            var sourceClosed = source.ToDisplayString(Fq);

            var seed = row.Seed switch
            {
                SeedKind.Ctor => $"new {sourceClosed}(source.Context)",
                SeedKind.FromContext => $"{sourceClosed}.FromContext(source.Context)",
                SeedKind.UpcastCtor => $"new {sourceClosed}(Upcast<{shapeType}, {string.Format(row.UpcastTargetFormat!, typeParamNames)}>(source.Context, x => x))",
                _ => $"new {sourceClosed}(source.Context)",
            };

            var sourceParamNames = new HashSet<string>(source.TypeParameters.Select(t => t.Name));
            var sourceConstraints = source.TypeParameters
                .Select(Constraint)
                .Where(c => c is not null)
                .Select(c => c!)
                .ToList();
            var sourceGenericList = string.Join(", ", source.TypeParameters.Select(t => t.Name));

            var methods = CollectMethods(source, sourceParamNames)
                .Select(m => RenderMethod(m, receiver, seed, sourceGenericList, sourceConstraints))
                .ToImmutableEquatableArray();

            shapes.Add(new ShapeModel(receiver, row.NetGuard, methods));
        }

        return new DrillInModel(target.Name, wrapperOpen, shapes.ToImmutableEquatableArray());
    }

    // Walk the source's base chain (stopping before Assertion<T>), collecting public ordinary instance
    // methods, excluding infrastructure and [Obsolete] members, and deduping new-overrides most-derived-wins.
    private static List<IMethodSymbol> CollectMethods(INamedTypeSymbol source, HashSet<string> sourceParamNames)
    {
        var result = new List<IMethodSymbol>();
        var seen = new HashSet<string>();

        for (INamedTypeSymbol? cur = source; cur is not null; cur = cur.BaseType)
        {
            if (cur.Name == "Assertion" && cur.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core")
            {
                break;
            }

            if (cur.SpecialType == SpecialType.System_Object)
            {
                break;
            }

            foreach (var method in cur.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.MethodKind != MethodKind.Ordinary
                    || method.DeclaredAccessibility != Accessibility.Public
                    || method.IsStatic
                    || ExcludedNames.Contains(method.Name))
                {
                    continue;
                }

                if (method.GetAttributes().Any(a => a.AttributeClass?.Name == "ObsoleteAttribute"))
                {
                    continue;
                }

                // Skip a method whose own type parameter collides with a source type parameter
                // (e.g. dictionary IsOrderedBy<TKey> vs the source's TKey) — it cannot be forwarded
                // without renaming, and is a niche operation on the affected shapes.
                if (method.TypeParameters.Any(tp => sourceParamNames.Contains(tp.Name)))
                {
                    continue;
                }

                var key = method.Name + "(" + string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(Fq))) + ")";
                if (seen.Add(key))
                {
                    result.Add(method);
                }
            }
        }

        return result;
    }

    private static ForwardMethod RenderMethod(
        IMethodSymbol method, string receiver, string seed, string sourceGenericList, List<string> sourceConstraints)
    {
        var returnType = method.ReturnType.ToDisplayString(Fq);

        var methodGenerics = method.TypeParameters.Select(t => t.Name).ToList();
        var allGenerics = new List<string> { sourceGenericList };
        allGenerics.AddRange(methodGenerics);
        var genericList = string.Join(", ", allGenerics.Where(g => g.Length > 0));

        var paramList = string.Join(", ", method.Parameters.Select(RenderParameter));

        var constraints = new List<string>(sourceConstraints);
        foreach (var tp in method.TypeParameters)
        {
            var c = Constraint(tp);
            if (c is not null)
            {
                constraints.Add(c);
            }
        }

        var orp = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "OverloadResolutionPriorityAttribute");
        var orpLine = orp is { ConstructorArguments.Length: > 0 } && orp.ConstructorArguments[0].Value is int p && p != 0
            ? $"    [global::System.Runtime.CompilerServices.OverloadResolutionPriority({p})]\n"
            : "";

        var typeArgs = methodGenerics.Count > 0 ? $"<{string.Join(", ", methodGenerics)}>" : "";
        var forwardArgs = string.Join(", ", method.Parameters.Select(p => p.Name));

        var signature =
            $"{orpLine}    public static {returnType} {method.Name}<{genericList}>(this {receiver} source"
            + (paramList.Length > 0 ? ", " + paramList : "")
            + ")"
            + (constraints.Count > 0 ? " " + string.Join(" ", constraints) : "");

        var body = $"        => {seed}.{method.Name}{typeArgs}({forwardArgs});";

        return new ForwardMethod(signature, body);
    }

    private static string RenderParameter(IParameterSymbol p)
    {
        var sb = new StringBuilder();

        var cae = p.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "CallerArgumentExpressionAttribute");
        if (cae is { ConstructorArguments.Length: > 0 } && cae.ConstructorArguments[0].Value is string target)
        {
            sb.Append($"[global::System.Runtime.CompilerServices.CallerArgumentExpression(\"{target}\")] ");
        }

        switch (p.RefKind)
        {
            case RefKind.Ref: sb.Append("ref "); break;
            case RefKind.Out: sb.Append("out "); break;
            case RefKind.In: sb.Append("in "); break;
        }

        if (p.IsParams)
        {
            sb.Append("params ");
        }

        sb.Append(p.Type.ToDisplayString(Fq));
        sb.Append(' ');
        sb.Append(p.Name);

        if (p.HasExplicitDefaultValue)
        {
            sb.Append(" = ");
            sb.Append(DefaultValueFormatter.FormatDefaultValueFullyQualified(p.ExplicitDefaultValue, p.Type));
        }

        return sb.ToString();
    }

    private static string? Constraint(ITypeParameterSymbol tp)
    {
        var cs = new List<string>();
        if (tp.HasReferenceTypeConstraint) cs.Add("class");
        if (tp.HasValueTypeConstraint) cs.Add("struct");
        if (tp.HasNotNullConstraint) cs.Add("notnull");
        if (tp.HasUnmanagedTypeConstraint) cs.Add("unmanaged");
        foreach (var c in tp.ConstraintTypes)
        {
            cs.Add(c.ToDisplayString(Fq));
        }
        if (tp.HasConstructorConstraint) cs.Add("new()");

        return cs.Count > 0 ? $"where {tp.Name} : {string.Join(", ", cs)}" : null;
    }

    // ---------------------------------------------------------------------------------------------------
    // Emit
    // ---------------------------------------------------------------------------------------------------

    private static void EmitDrillIn(SourceProductionContext context, DrillInModel model)
    {
        var sb = StartGeneratedFile();
        sb.AppendLine($"public static partial class {model.WrapperName}CollectionShapeDrillInExtensions");
        sb.AppendLine("{");

        // Shared upcast helper: carries an already-captured PendingPreWork across an identity context map so
        // a concrete shape (List<T>/Dictionary<K,V>) still runs the drill-in's pre-work (e.g. the ContainsKey
        // check) before reading the value.
        sb.AppendLine("    private static global::TUnit.Assertions.Core.AssertionContext<TTo> Upcast<TFrom, TTo>(");
        sb.AppendLine("        global::TUnit.Assertions.Core.AssertionContext<TFrom> context, global::System.Func<TFrom?, TTo?> upcast)");
        sb.AppendLine("    {");
        sb.AppendLine("        var mapped = context.Map(upcast);");
        sb.AppendLine("        if (context.PendingPreWork is { } preWork)");
        sb.AppendLine("        {");
        sb.AppendLine("            var existing = mapped.PendingPreWork;");
        sb.AppendLine("            mapped.PendingPreWork = existing is null");
        sb.AppendLine("                ? preWork");
        sb.AppendLine("                : async () => { await existing(); await preWork(); };");
        sb.AppendLine("        }");
        sb.AppendLine("        return mapped;");
        sb.AppendLine("    }");

        foreach (var shape in model.Shapes)
        {
            if (shape.Methods.Count == 0)
            {
                continue;
            }

            sb.AppendLine();
            if (shape.NetGuard is not null)
            {
                sb.AppendLine($"#if {shape.NetGuard}");
            }

            sb.AppendLine($"    // {shape.ReceiverType}");
            foreach (var m in shape.Methods)
            {
                sb.AppendLine(m.Signature);
                sb.AppendLine(m.Body);
            }

            if (shape.NetGuard is not null)
            {
                sb.AppendLine("#endif");
            }
        }

        sb.AppendLine("}");

        context.AddSource($"{model.WrapperName}.CollectionShapeDrillIns.g.cs", sb.ToString());
    }
}
