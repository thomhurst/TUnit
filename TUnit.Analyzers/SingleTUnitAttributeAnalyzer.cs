using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SingleTUnitAttributeAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        new()
        {
            Rules.DuplicateSingleAttribute
        };

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;

        var attributes = symbol.GetAttributes();

        var singleAttributes = attributes.Select(ToClassInheritingSingleAttribute).OfType<INamedTypeSymbol>().ToList();

        var notDistinctAttributes = singleAttributes.GroupBy(x => x, SymbolEqualityComparer.Default).Where(x => x.Count() > 1).Select(x => x.Key!).ToList();

        foreach (var notDistinctAttribute in notDistinctAttributes)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.DuplicateSingleAttribute, symbol.Locations.FirstOrDefault(), notDistinctAttribute));
        }
    }

    private INamedTypeSymbol? ToClassInheritingSingleAttribute(AttributeData attributeData)
    {
        var typeWithBases = attributeData.AttributeClass?.GetSelfAndBaseTypes().ToList();

        var index = typeWithBases?.FindIndex(x => x?.GloballyQualified() == "global::TUnit.Core.SingleTUnitAttribute");

        if (index is null or -1 or 0)
        {
            return null;
        }

        return typeWithBases?.ElementAtOrDefault(index.Value - 1);
    }

    private bool IsType(List<INamedTypeSymbol>? namedTypeSymbols, INamedTypeSymbol? attributeClass)
    {
        if (attributeClass is null || namedTypeSymbols is null)
        {
            return false;
        }

        return namedTypeSymbols.Any(x => SymbolEqualityComparer.Default.Equals(x, attributeClass));
    }
}
