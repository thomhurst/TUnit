using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                WellKnown.AttributeFullyQualifiedClasses.InheritsTestsAttribute))
        {
            return;
        }

        var methods = namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Skip(1)
            .SelectMany(x => x.GetMembers())
            .OfType<IMethodSymbol>();

        if (methods.Any(HasTestAttribute))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.DoesNotInheritTestsWarning,
                namedTypeSymbol.Locations.FirstOrDefault())
            );
        }
    }

    private static bool HasTestAttribute(IMethodSymbol m)
    {
        return m.GetAttributes().Any(IsTestAttribute);
    }

    private static bool IsTestAttribute(AttributeData x)
    {
        var qualifiedName = x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

        return WellKnown.AttributeFullyQualifiedClasses.TestAttributes.Contains(qualifiedName);
    }
}