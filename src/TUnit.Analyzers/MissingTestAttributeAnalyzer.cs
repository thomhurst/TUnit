using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingTestAttributeAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MissingTestAttribute);

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

        var symbols = TUnitSymbols.For(context.Compilation);

        // Base types from assemblies that don't reference TUnit.Core (e.g. BCL types) can't carry TUnit data
        // source attributes, so don't decode the attributes of all their members.
        var methods = namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Where(symbols.MayContainTUnitMembers)
            .SelectMany(x => x.GetMembers())
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary)
            .Where(x => !x.IsStatic);

        // IsTestMethod is the cheaper check and rules out the vast majority of methods, so evaluate it first.
        foreach (var method in methods.Where(x => !symbols.IsTestMethod(x) && x.HasDataDrivenAttributes()))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MissingTestAttribute,
                method.Locations.FirstOrDefault())
            );
        }
    }
}
