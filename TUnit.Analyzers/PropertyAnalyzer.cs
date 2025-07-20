using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Rules.TooManyDataAttributes
    ];

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol propertySymbol)
        {
            return;
        }

        if (propertySymbol.GetAttributes().Count(x => x.IsDataSourceAttribute(context.Compilation)) > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.TooManyDataAttributes,
                propertySymbol.Locations.FirstOrDefault())
            );
        }
    }
}
