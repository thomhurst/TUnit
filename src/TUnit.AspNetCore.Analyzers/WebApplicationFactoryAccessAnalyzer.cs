using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.AspNetCore.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WebApplicationFactoryAccessAnalyzer : ConcurrentDiagnosticAnalyzer
{
    // Properties not available in constructors OR SetupAsync (initialized in Before hook)
    private static bool IsRestrictedInConstructorAndSetup(string name) => name is "Factory" or "Services" or "HttpCapture";

    // Properties not available in constructors only (available after property injection, before SetupAsync)
    private static bool IsRestrictedInConstructorOnly(string name) => name is "GlobalFactory";

    // Members that should never be accessed on GlobalFactory (breaks test isolation)
    private static bool IsRestrictedGlobalFactoryMember(string name) => name is "Services" or "Server" or "CreateClient" or "CreateDefaultClient";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.FactoryAccessedTooEarly, Rules.GlobalFactoryMemberAccess);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Every diagnostic needs a member declared on TUnit.AspNetCore.WebApplicationTest (or a derived type),
            // so skip compilations that can't see any type of that name.
            if (!ReferencesWebApplicationTest(compilationContext.Compilation))
            {
                return;
            }

            compilationContext.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
            compilationContext.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private static bool ReferencesWebApplicationTest(Compilation compilation)
    {
        // GetTypesByMetadataName searches the source assembly and every reference, including ones only
        // reachable through an extern alias (which Compilation.GlobalNamespace omits), and unlike
        // GetTypeByMetadataName it doesn't return null when several assemblies declare the type.
        // Both arities are checked to match IsWebApplicationTestType's arity-agnostic name check.
        return !compilation.GetTypesByMetadataName("TUnit.AspNetCore.WebApplicationTest").IsEmpty
            || !compilation.GetTypesByMetadataName("TUnit.AspNetCore.WebApplicationTest`2").IsEmpty;
    }

    private void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation propertyReference)
        {
            return;
        }

        var propertyName = propertyReference.Property.Name;

        // Check for GlobalFactory.Services or GlobalFactory.Server access
        if (IsRestrictedGlobalFactoryMember(propertyName) &&
            IsGlobalFactoryAccess(propertyReference.Instance))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rules.GlobalFactoryMemberAccess,
                context.Operation.Syntax.GetLocation(),
                propertyName));
            return;
        }

        var isRestrictedInBoth = IsRestrictedInConstructorAndSetup(propertyName);
        var isRestrictedInConstructorOnly = IsRestrictedInConstructorOnly(propertyName);

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

        // Check if we're in a constructor or SetupAsync method. The operation block's owning symbol is the
        // enclosing member (also for lambdas and local functions inside it), so no need to re-bind its declaration.
        if (context.ContainingSymbol is not IMethodSymbol containingMethod)
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
        if (IsRestrictedGlobalFactoryMember(methodName) &&
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
            // Check for WebApplicationTest or WebApplicationTest<TFactory, TEntryPoint>
            // (a constructed generic shares its definition's name and namespace).
            if (type.Name == "WebApplicationTest"
                && type.ContainingNamespace is { Name: "AspNetCore", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } })
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }
}
