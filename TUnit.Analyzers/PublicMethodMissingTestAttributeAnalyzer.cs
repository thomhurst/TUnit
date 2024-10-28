using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PublicMethodMissingTestAttributeAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            Rules.PublicMethodMissingTestAttribute
        );

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

        var methods = namedTypeSymbol.GetMembers().OfType<IMethodSymbol>().ToArray();
        
        if (!methods.Any(x => x.IsTestMethod(context.Compilation)))
        {
            return;
        }
        
        foreach (var method in methods
                     .Where(x => x.MethodKind == MethodKind.Ordinary)
                     .Where(x => !x.IsAbstract)
                     .Where(x => !x.IsStatic)
                     .Where(x => !x.IsOverride)
                     .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                     .Where(x => !x.IsTestMethod(context.Compilation))
                     .Where(x => !x.IsNonGlobalHookMethod(context.Compilation))
                     .Where(x => !IsDisposableDispose(x))
                     .Where(x => !IsAsyncDisposableDispose(x)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.PublicMethodMissingTestAttribute, method.Locations.FirstOrDefault()));
        }
    }

    private bool IsDisposableDispose(IMethodSymbol method)
    {
        return method is { ReturnsVoid: true, Name: "Dispose" } &&
               method.ContainingType.AllInterfaces.Any(x => x.SpecialType == SpecialType.System_IDisposable);
    }
    
    private bool IsAsyncDisposableDispose(IMethodSymbol method)
    {
        return method is { ReturnsVoid: false, Name: "DisposeAsync" };
    }
}