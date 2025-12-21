using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.AspNetCore.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WebApplicationFactoryAccessAnalyzer : ConcurrentDiagnosticAnalyzer
{
    // Properties not available in constructors OR SetupAsync (initialized in Before hook)
    private static readonly ImmutableHashSet<string> RestrictedInConstructorAndSetup = ImmutableHashSet.Create(
        "Factory",
        "Services",
        "HttpCapture"
    );

    // Properties not available in constructors only (available after property injection, before SetupAsync)
    private static readonly ImmutableHashSet<string> RestrictedInConstructorOnly = ImmutableHashSet.Create(
        "GlobalFactory"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.FactoryAccessedTooEarly);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
    }

    private void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation propertyReference)
        {
            return;
        }

        var propertyName = propertyReference.Property.Name;

        var isRestrictedInBoth = RestrictedInConstructorAndSetup.Contains(propertyName);
        var isRestrictedInConstructorOnly = RestrictedInConstructorOnly.Contains(propertyName);

        if (!isRestrictedInBoth && !isRestrictedInConstructorOnly)
        {
            return;
        }

        // Check if this property belongs to WebApplicationTest or a derived type
        var containingType = propertyReference.Property.ContainingType;
        if (!IsWebApplicationTestType(containingType))
        {
            return;
        }

        // Check if we're in a constructor or SetupAsync method
        var containingMethod = GetContainingMethod(context.Operation);
        if (containingMethod == null)
        {
            return;
        }

        string? contextName = null;

        if (containingMethod.MethodKind == MethodKind.Constructor)
        {
            // All restricted properties are invalid in constructor
            contextName = "constructor";
        }
        else if (containingMethod.Name == "SetupAsync" && containingMethod.IsOverride)
        {
            // Only Factory/Services/HttpCapture are invalid in SetupAsync
            // GlobalFactory IS available in SetupAsync
            if (isRestrictedInBoth)
            {
                contextName = "SetupAsync";
            }
        }

        if (contextName != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rules.FactoryAccessedTooEarly,
                context.Operation.Syntax.GetLocation(),
                propertyName,
                contextName));
        }
    }

    private static bool IsWebApplicationTestType(INamedTypeSymbol? type)
    {
        while (type != null)
        {
            var typeName = type.Name;
            var namespaceName = type.ContainingNamespace?.ToDisplayString();

            // Check for WebApplicationTest or WebApplicationTest<TFactory, TEntryPoint>
            if (typeName == "WebApplicationTest" && namespaceName == "TUnit.AspNetCore")
            {
                return true;
            }

            // Also check the generic version
            if (type.OriginalDefinition?.Name == "WebApplicationTest" &&
                type.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "TUnit.AspNetCore")
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    private static IMethodSymbol? GetContainingMethod(IOperation operation)
    {
        var current = operation;
        while (current != null)
        {
            if (current is IMethodBodyOperation or IBlockOperation)
            {
                // Get the semantic model to find the containing method
                var syntax = current.Syntax;
                while (syntax != null)
                {
                    if (syntax is MethodDeclarationSyntax or ConstructorDeclarationSyntax)
                    {
                        var semanticModel = operation.SemanticModel;
                        if (semanticModel != null)
                        {
                            var symbol = semanticModel.GetDeclaredSymbol(syntax);
                            if (symbol is IMethodSymbol methodSymbol)
                            {
                                return methodSymbol;
                            }
                        }
                    }
                    syntax = syntax.Parent;
                }
            }
            current = current.Parent;
        }

        return null;
    }
}
