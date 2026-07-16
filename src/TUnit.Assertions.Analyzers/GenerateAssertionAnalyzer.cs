using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Analyzer that validates proper usage of the [GenerateAssertion] attribute.
/// Ensures methods meet requirements: static, have parameters, return correct types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GenerateAssertionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.GenerateAssertionMethodMustBeStatic,
            Rules.GenerateAssertionMethodMustHaveParameter,
            Rules.GenerateAssertionInvalidReturnType,
            Rules.GenerateAssertionShouldBeExtensionMethod
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        // Check if method has [GenerateAssertion] attribute
        var hasGenerateAssertionAttribute = methodSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == "TUnit.Assertions.Attributes.GenerateAssertionAttribute");

        if (!hasGenerateAssertionAttribute)
        {
            return;
        }

        // Rule 1: Method must be static
        if (!methodSymbol.IsStatic)
        {
            var diagnostic = Diagnostic.Create(
                Rules.GenerateAssertionMethodMustBeStatic,
                methodSymbol.Locations[0],
                methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        // Rule 2: Method must have at least one parameter
        if (methodSymbol.Parameters.Length == 0)
        {
            var diagnostic = Diagnostic.Create(
                Rules.GenerateAssertionMethodMustHaveParameter,
                methodSymbol.Locations[0],
                methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        // Rule 3: Method must return valid type
        if (!IsValidReturnType(methodSymbol.ReturnType))
        {
            var diagnostic = Diagnostic.Create(
                Rules.GenerateAssertionInvalidReturnType,
                methodSymbol.Locations[0],
                methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        // Rule 4 (Warning): Method should be extension method
        if (methodSymbol.Parameters.Length > 0 && !methodSymbol.IsExtensionMethod)
        {
            var diagnostic = Diagnostic.Create(
                Rules.GenerateAssertionShouldBeExtensionMethod,
                methodSymbol.Locations[0],
                methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsValidReturnType(ITypeSymbol returnType)
    {
        // Check for bool
        if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            return true;
        }

        // Check for AssertionResult
        if (returnType is INamedTypeSymbol namedReturnType)
        {
            if (namedReturnType.Name == "AssertionResult" &&
                namedReturnType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core")
            {
                return true;
            }

            // Check for Task<bool> or Task<AssertionResult>
            if (namedReturnType.Name == "Task" &&
                namedReturnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks" &&
                namedReturnType.TypeArguments.Length == 1)
            {
                var innerType = namedReturnType.TypeArguments[0];

                // Task<bool>
                if (innerType.SpecialType == SpecialType.System_Boolean)
                {
                    return true;
                }

                // Task<AssertionResult>
                if (innerType is INamedTypeSymbol innerNamedType &&
                    innerNamedType.Name == "AssertionResult" &&
                    innerNamedType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core")
                {
                    return true;
                }
            }
        }

        return false;
    }
}
