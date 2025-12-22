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

    // Members that should never be accessed on GlobalFactory (breaks test isolation)
    private static readonly ImmutableHashSet<string> RestrictedGlobalFactoryMembers = ImmutableHashSet.Create(
        "Services",
        "Server",
        "CreateClient"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.FactoryAccessedTooEarly, Rules.GlobalFactoryMemberAccess);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation propertyReference)
        {
            return;
        }

        var propertyName = propertyReference.Property.Name;

        // Check for GlobalFactory.Services or GlobalFactory.Server access
        if (RestrictedGlobalFactoryMembers.Contains(propertyName) &&
            IsGlobalFactoryAccess(propertyReference.Instance))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rules.GlobalFactoryMemberAccess,
                context.Operation.Syntax.GetLocation(),
                propertyName));
            return;
        }

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

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var methodName = invocation.TargetMethod.Name;

        // Check for GlobalFactory.CreateClient() access
        if (RestrictedGlobalFactoryMembers.Contains(methodName) &&
            IsGlobalFactoryAccess(invocation.Instance))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rules.GlobalFactoryMemberAccess,
                context.Operation.Syntax.GetLocation(),
                methodName));
        }
    }

    private static bool IsGlobalFactoryAccess(IOperation? instance)
    {
        if (instance is not IPropertyReferenceOperation propertyRef)
        {
            return false;
        }

        // Check if accessing GlobalFactory property on a WebApplicationTest type
        if (propertyRef.Property.Name != "GlobalFactory")
        {
            return false;
        }

        return IsWebApplicationTestType(propertyRef.Property.ContainingType);
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
