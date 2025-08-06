using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MultipleConstructorsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MultipleConstructorsWithoutTestConstructorDescriptor
        = new("TUnit0052",
            "Multiple constructors found without [TestConstructor] attribute",
            "Class '{0}' has multiple constructors but none are marked with [TestConstructor]. Consider adding [TestConstructor] to specify which constructor to use.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "When a test class has multiple constructors, one should be marked with [TestConstructor] to avoid ambiguity.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(MultipleConstructorsWithoutTestConstructorDescriptor);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(RegisterActions);
    }

    private void RegisterActions(CompilationStartAnalysisContext context)
    {
        context.RegisterSymbolAction(analysisContext => AnalyzeTypeSymbol(analysisContext, context.Compilation), SymbolKind.NamedType);
    }

    private void AnalyzeTypeSymbol(SymbolAnalysisContext context, Compilation compilation)
    {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        // Only analyze test classes
        if (!namedTypeSymbol.IsTestClass(compilation))
        {
            return;
        }

        // Get all instance constructors
        var constructors = namedTypeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
            .ToList();

        // If there's only one constructor or no constructors, no ambiguity
        if (constructors.Count <= 1)
        {
            return;
        }

        // Check if any constructor has [TestConstructor] attribute
        var hasTestConstructorAttribute = constructors.Any(c =>
            c.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == "TUnit.Core.TestConstructorAttribute"));

        // If multiple constructors but no [TestConstructor] attribute, report warning
        if (!hasTestConstructorAttribute)
        {
            var diagnostic = Diagnostic.Create(
                MultipleConstructorsWithoutTestConstructorDescriptor,
                namedTypeSymbol.Locations.FirstOrDefault(),
                namedTypeSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}