using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static TUnit.Assertions.SourceGenerator.Generators.CollectionShapeRegistry;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Generates per-collection-shape overloads of a single marked generic method, one overload per shape in
/// <see cref="CollectionShapeRegistry"/>:
/// <list type="bullet">
/// <item><c>[GenerateCollectionShapeSatisfiesOverloads]</c> → the <c>Satisfies</c> overloads that bind the
/// most specific assertion source for the user lambda (replaces <c>CollectionItemSatisfiesExtensions.cs</c>).</item>
/// <item><c>[GenerateCollectionShapeCountOverloads]</c> → the <c>Count(itemAssertion)</c> overloads whose item
/// lambda receives the per-item shape source (replaces the #5707 block in <c>AssertionExtensions.cs</c>).</item>
/// </list>
/// Both are fixed templates (the signature is identical across shapes), so unlike
/// <see cref="CollectionShapeAssertionGenerator"/> they do not reflect a method surface. See issue #6185.
/// </summary>
[Generator]
public sealed class CollectionShapeOverloadGenerator : IIncrementalGenerator
{
    private const string SatisfiesAttribute = "TUnit.Assertions.Attributes.GenerateCollectionShapeSatisfiesOverloadsAttribute";
    private const string CountAttribute = "TUnit.Assertions.Attributes.GenerateCollectionShapeCountOverloadsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect() so each single output file is emitted exactly once even if more than one method ever carries
        // the trigger attribute (two RegisterSourceOutput firings would add the same hint name twice — CS8785).
        var satisfiesTrigger = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SatisfiesAttribute,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (_, _) => true)
            .WithTrackingName("CollectionShapeSatisfiesTrigger")
            .Collect();
        context.RegisterSourceOutput(satisfiesTrigger, static (spc, triggers) =>
        {
            if (triggers.Length > 0)
            {
                EmitSatisfiesOverloads(spc);
            }
        });

        var countTrigger = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                CountAttribute,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (_, _) => true)
            .WithTrackingName("CollectionShapeCountTrigger")
            .Collect();
        context.RegisterSourceOutput(countTrigger, static (spc, triggers) =>
        {
            if (triggers.Length > 0)
            {
                EmitCountOverloads(spc);
            }
        });
    }

    private static void EmitSatisfiesOverloads(SourceProductionContext context)
    {
        var sb = StartGeneratedFile(netStandardOnly: true);
        sb.AppendLine("/// <summary>Generated per-collection-shape <c>Satisfies</c> overloads (issue #6185).</summary>");
        sb.AppendLine("public static class CollectionItemSatisfiesExtensions");
        sb.AppendLine("{");

        foreach (var s in ItemShapes)
        {
            if (s.NetGuard is not null)
            {
                sb.AppendLine($"#if {s.NetGuard}");
            }
            sb.AppendLine($"    public static TResult Satisfies<{s.Names}, TResult>(");
            sb.AppendLine($"        this global::TUnit.Assertions.Core.IItemSatisfiesSource<{s.Shape}, TResult> source,");
            sb.AppendLine($"        global::System.Func<{s.SourceClosed}, global::TUnit.Assertions.Core.IAssertion?> assertion,");
            sb.Append("        [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"assertion\")] string? expression = null)");
            sb.Append(s.DictConstraint);
            sb.AppendLine();
            sb.AppendLine($"        => source.Satisfies<{s.SourceClosed}>(assertion, expression);");
            if (s.NetGuard is not null)
            {
                sb.AppendLine("#endif");
            }
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
            if (s.NetGuard is not null)
            {
                sb.AppendLine($"#if {s.NetGuard}");
            }
            sb.AppendLine($"    public static global::TUnit.Assertions.Conditions.CollectionCountSource<TCollection, {s.Shape}> Count<TCollection, {s.Names}>(");
            sb.AppendLine($"        this global::TUnit.Assertions.Sources.CollectionAssertionBase<TCollection, {s.Shape}> source,");
            sb.AppendLine($"        global::System.Func<{s.SourceClosed}, global::TUnit.Assertions.Core.IAssertion?> itemAssertion,");
            sb.AppendLine("        [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"itemAssertion\")] string? expression = null)");
            sb.Append($"        where TCollection : global::System.Collections.Generic.IEnumerable<{s.Shape}>");
            sb.Append(s.DictConstraint);
            sb.AppendLine();
            sb.AppendLine($"        => CountSpecialised<TCollection, {s.Shape}>(source, (item, index) => itemAssertion(new {s.SourceClosed}(item, $\"item[{{index}}]\")), expression);");
            if (s.NetGuard is not null)
            {
                sb.AppendLine("#endif");
            }
        }

        sb.AppendLine("}");
        context.AddSource("AssertionExtensions.CollectionShapeCount.g.cs", sb.ToString());
    }
}
