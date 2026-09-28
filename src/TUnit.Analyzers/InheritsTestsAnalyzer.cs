using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InheritsTestsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.DoesNotInheritTestsWarning);

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

        if (namedTypeSymbol.GetAttributes().Any(x =>
                x.AttributeClass?.IsGloballyQualified(
                WellKnown.AttributeFullyQualifiedClasses.InheritsTestsAttribute.WithGlobalPrefix) == true))
        {
            return;
        }

        var symbols = TUnitSymbols.For(context.Compilation);

        // A compilation without [Test] can't have inherited tests.
        if (symbols.TestAttribute is null)
        {
            return;
        }

        // Base types from assemblies that don't reference TUnit.Core (e.g. BCL types) can't declare [Test] methods,
        // so don't decode the attributes of all their members.
        var methods = namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Skip(1)
            .Where(symbols.MayContainTUnitMembers)
            .SelectMany(x => x.GetMembers())
            .OfType<IMethodSymbol>();

        if (methods.Any(symbols.IsTestMethod))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.DoesNotInheritTestsWarning,
                namedTypeSymbol.Locations.FirstOrDefault())
            );
        }
    }
}
