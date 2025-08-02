using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PublicMethodMissingTestAttributeAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.PublicMethodMissingTestAttribute);

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

        var testMethods = methods.Where(x => x.IsTestMethod(context.Compilation)).ToArray();

        if (!testMethods.Any())
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
                     .Where(x => !x.IsStandardHookMethod(context.Compilation, out _, out _, out _))
                     .Where(x => !IsDisposableDispose(x))
                     .Where(x => !IsAsyncDisposableDispose(x))
                     .Where(x => !IsInitializeAsync(x))
                     .Where(x => !IsTestDataSource(x, testMethods)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.PublicMethodMissingTestAttribute, method.Locations.FirstOrDefault()));
        }
    }

    private bool IsTestDataSource(IMethodSymbol methodSymbol, IMethodSymbol[] testMethods)
    {
        var attributes = testMethods.SelectMany(x => x.GetAttributes())
            .Concat(testMethods.SelectMany(x => x.Parameters).SelectMany(x => x.GetAttributes()));

        if (attributes.Any(x => !x.ConstructorArguments.IsDefaultOrEmpty
                               && x.ConstructorArguments.Any(a => a is { Kind: TypedConstantKind.Primitive, Value: string s } && s == methodSymbol.Name)))
        {
            return true;
        }

        return false;
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

    private bool IsInitializeAsync(IMethodSymbol method)
    {
        return method is { ReturnsVoid: false, Name: "InitializeAsync" };
    }
}
