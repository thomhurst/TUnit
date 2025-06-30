using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using AttributeUsageAttribute = System.AttributeUsageAttribute;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ForbidRedefiningAttributeUsageAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        new()
        {
            Rules.DoNotOverrideAttributeUsageMetadata
        };

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        if (!namedTypeSymbol.IsOrInherits("global::TUnit.Core.TUnitAttribute"))
        {
            return;
        }

        if (namedTypeSymbol.ContainingNamespace?.ToDisplayString().StartsWith("TUnit.Core") == true)
        {
            return;
        }

        var attributeUsage = namedTypeSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.GloballyQualified() == $"global::{typeof(AttributeUsageAttribute).FullName}");

        if (attributeUsage == null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.DoNotOverrideAttributeUsageMetadata, attributeUsage?.GetLocation() ?? context.Symbol.Locations.FirstOrDefault()));
    }
}
