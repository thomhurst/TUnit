using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.SourceGenerator.Models;
using static TUnit.Assertions.SourceGenerator.Generators.CollectionShapeRegistry;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Generates the full-surface <c>.Value</c>-style wrapper assertions: a generic arity-1 assertion source marked
/// with <c>[GenerateCollectionShapeAssertions]</c> gets, for every collection shape in
/// <see cref="CollectionShapeRegistry"/>, one forwarding extension per public method of that shape's assertion
/// source — reflected from the real <see cref="IMethodSymbol"/> so per-shape signature differences (e.g. the
/// <c>IEqualityComparer</c> parameter on <c>ReadOnlyList.HasItemAt</c> that <c>IList.HasItemAt</c> lacks) are
/// always correct and new methods are picked up automatically. See issue #6185.
/// </summary>
[Generator]
public sealed class CollectionShapeAssertionGenerator : IIncrementalGenerator
{
    private const string AssertionsAttribute = "TUnit.Assertions.Attributes.GenerateCollectionShapeAssertionsAttribute";

    private static readonly SymbolDisplayFormat Fq = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    // Methods that must NOT be forwarded onto the wrapper source. A new assertion method added to a shape's
    // source class is forwarded automatically (the whole point); only add a name here if forwarding it would
    // be redundant or ambiguous, in one of these groups:
    private static readonly HashSet<string> ExcludedNames = new()
    {
        // Already declared on IAssertionSource<T> / ValueAssertion<T>, so the wrapper exposes them directly —
        // re-forwarding would create ambiguous overloads.
        "IsNull", "IsNotNull", "IsTypeOf", "IsNotTypeOf",
        "IsAssignableTo", "IsNotAssignableTo", "IsAssignableFrom", "IsNotAssignableFrom",
        // Assertion<T> lifecycle / evaluation infrastructure, not user-facing assertions.
        "GetExpectation", "CheckAsync", "AssertAsync", "GetAwaiter", "Because",
        // System.Object members.
        "Equals", "GetHashCode", "ToString", "GetType",
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var wrappers = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AssertionsAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => BuildWrapperModel(ctx, ct))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .WithTrackingName("CollectionShapeAssertionTargets");

        context.RegisterSourceOutput(wrappers, static (spc, model) => EmitWrapperAssertions(spc, model));
    }

    // ---------------------------------------------------------------------------------------------------
    // Model (fully equatable — no ISymbol stored)
    // ---------------------------------------------------------------------------------------------------

    private sealed record WrapperModel(
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

    private static WrapperModel? BuildWrapperModel(GeneratorAttributeSyntaxContext context, CancellationToken ct)
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
                SeedKind.UpcastCtor => $"new {sourceClosed}(source.Context.MapPreservingPreWork<{string.Format(row.UpcastTargetFormat!, typeParamNames)}>(x => x))",
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

        return new WrapperModel(target.Name, wrapperOpen, shapes.ToImmutableEquatableArray());
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
        if (tp.HasReferenceTypeConstraint)
        {
            cs.Add("class");
        }

        // 'unmanaged' also sets HasValueTypeConstraint; 'struct, unmanaged' is CS0449 (#6471)
        if (tp.HasValueTypeConstraint && !tp.HasUnmanagedTypeConstraint)
        {
            cs.Add("struct");
        }

        if (tp.HasUnmanagedTypeConstraint)
        {
            cs.Add("unmanaged");
        }

        if (tp.HasNotNullConstraint)
        {
            cs.Add("notnull");
        }

        foreach (var c in tp.ConstraintTypes)
        {
            cs.Add(c.ToDisplayString(Fq));
        }

        if (tp.HasConstructorConstraint)
        {
            cs.Add("new()");
        }

        return cs.Count > 0 ? $"where {tp.Name} : {string.Join(", ", cs)}" : null;
    }

    // ---------------------------------------------------------------------------------------------------
    // Emit
    // ---------------------------------------------------------------------------------------------------

    private static void EmitWrapperAssertions(SourceProductionContext context, WrapperModel model)
    {
        var sb = StartGeneratedFile();
        sb.AppendLine($"public static partial class {model.WrapperName}CollectionShapeAssertions");
        sb.AppendLine("{");

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

        context.AddSource($"{model.WrapperName}.CollectionShapeAssertions.g.cs", sb.ToString());
    }
}
