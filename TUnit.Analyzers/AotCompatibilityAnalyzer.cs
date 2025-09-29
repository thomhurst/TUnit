using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AotCompatibilityAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.GenericTypeNotAotCompatible,
            Rules.TupleNotAotCompatible,
            Rules.CustomConversionNotAotCompatible);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSymbolAction(AnalyzeTestMethod, SymbolKind.Method);
    }

    private void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        // Check for MakeGenericType calls
        if (methodSymbol.Name == "MakeGenericType" && IsInTestContext(invocation, context))
        {
            // Check if this is in a source-generated or reflection-specific class
            var containingType = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart)?.ContainingType;
            if (containingType != null)
            {
                var typeName = containingType.Name;
                // Don't warn for known reflection-mode classes
                if (typeName.Contains("Reflection") ||
                    typeName.Contains("Dynamic") ||
                    typeName.EndsWith("DataCollector") ||
                    containingType.GetAttributes().Any(a => a.AttributeClass?.Name == "RequiresUnreferencedCodeAttribute"))
                {
                    return; // This is expected in reflection mode
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rules.GenericTypeNotAotCompatible,
                invocation.GetLocation(),
                "MakeGenericType"));
        }

        // Check for tuple operations (GetFields, GetProperties on tuple types)
        if ((methodSymbol.Name == "GetFields" || methodSymbol.Name == "GetProperties") && 
            IsInTestContext(invocation, context))
        {
            // Check if called directly on a tuple type
            if (IsCalledOnTupleType(invocation, context))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.TupleNotAotCompatible,
                    invocation.GetLocation(),
                    "Tuple reflection"));
            }
            // Check if called on a Type object that represents a tuple type
            else if (IsCalledOnTypeRepresentingTuple(invocation, context))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.TupleNotAotCompatible,
                    invocation.GetLocation(),
                    "Tuple reflection"));
            }
        }

        // Check for custom conversion operators
        if (methodSymbol.Name == "Invoke" && IsCustomConversionOperator(invocation, context))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rules.CustomConversionNotAotCompatible,
                invocation.GetLocation(),
                "Custom conversion operator"));
        }
    }

    private void AnalyzeTestMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!methodSymbol.IsTestMethod(context.Compilation))
        {
            return;
        }

        // Check if this is in source-generated mode (AOT-safe)
        if (IsSourceGeneratedMode(context.Compilation))
        {
            // Source generation handles generics at compile time - no warning needed
            return;
        }

        // Check if test method has generic parameters
        if (methodSymbol.IsGenericMethod || methodSymbol.ContainingType.IsGenericType)
        {
            // Check if all generic arguments are concrete types (known at compile time)
            if (HasOnlyConcreteTypeArguments(methodSymbol))
            {
                // Concrete types are AOT-safe - no warning
                return;
            }

            // Check if the method or class has the AotCompatible attribute
            if (HasAotCompatibleAttribute(methodSymbol))
            {
                // Method has been marked as AOT-safe - no warning
                return;
            }

            // Check if the test uses data sources that might require runtime type creation
            var hasDataSource = methodSymbol.GetAttributes()
                .Any(attr => IsDataSourceAttribute(attr, context.Compilation));

            if (hasDataSource)
            {
                // Only warn if the data source provides type arguments dynamically
                if (HasDynamicTypeArguments(methodSymbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rules.GenericTypeNotAotCompatible,
                        methodSymbol.Locations.FirstOrDefault(),
                        "Generic test method with dynamic type arguments"));
                }
            }
        }

        // Check for tuple parameters only if not using ITuple interface
        #if !NET
        foreach (var parameter in methodSymbol.Parameters)
        {
            if (IsTupleType(parameter.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.TupleNotAotCompatible,
                    parameter.Locations.FirstOrDefault(),
                    $"Tuple parameter '{parameter.Name}' - consider using concrete types for AOT compatibility"));
            }
        }
        #endif
    }

    private static bool IsSourceGeneratedMode(Compilation compilation)
    {
        // Check if SourceRegistrar.IsEnabled would be true
        // This can be determined by checking for source-generated assemblies
        var sourceRegistrar = compilation.GetTypeByMetadataName("TUnit.Core.SourceRegistrar");
        if (sourceRegistrar == null)
        {
            return false;
        }

        // Check for presence of source-generated test sources
        var testSourceInterface = compilation.GetTypeByMetadataName("TUnit.Core.Interfaces.SourceGenerator.ITestSource");
        if (testSourceInterface == null)
        {
            return false;
        }

        // If we find any implementations of ITestSource in the compilation, source generation is active
        // Check global namespace and all nested namespaces for types implementing ITestSource
        return CheckNamespaceForTestSources(compilation.Assembly.GlobalNamespace, testSourceInterface);
    }

    private static bool CheckNamespaceForTestSources(INamespaceSymbol namespaceSymbol, INamedTypeSymbol testSourceInterface)
    {
        // Check types in current namespace
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamedTypeSymbol type)
            {
                if (type.AllInterfaces.Contains(testSourceInterface, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }
        }

        // Recursively check nested namespaces
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol nestedNamespace)
            {
                if (CheckNamespaceForTestSources(nestedNamespace, testSourceInterface))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasOnlyConcreteTypeArguments(IMethodSymbol methodSymbol)
    {
        // Check all Arguments attributes to see if they specify concrete types
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "ArgumentsAttribute" ||
                attr.AttributeClass?.Name == "MethodDataSourceAttribute")
            {
                // Check if any argument is typeof(T) where T is generic
                foreach (var arg in attr.ConstructorArguments)
                {
                    if (arg.Value is ITypeSymbol typeSymbol &&
                        (typeSymbol.TypeKind == TypeKind.TypeParameter ||
                         typeSymbol is INamedTypeSymbol { IsGenericType: true }))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private static bool HasDynamicTypeArguments(IMethodSymbol methodSymbol)
    {
        // Check if type arguments come from runtime values
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "MethodDataSourceAttribute")
            {
                // Method data sources might provide types at runtime
                return true;
            }

            if (attr.AttributeClass?.Name == "ClassDataSourceAttribute")
            {
                // Class data sources might provide types at runtime
                return true;
            }

            if (attr.AttributeClass?.Name == "ArgumentsAttribute")
            {
                // Check if Arguments contains Type.GetType or other runtime type resolution
                foreach (var arg in attr.ConstructorArguments)
                {
                    if (arg.Value?.ToString()?.Contains("GetType") == true)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool IsInTestContext(SyntaxNode node, SyntaxNodeAnalysisContext context)
    {
        var containingMethod = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod == null)
        {
            return false;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(containingMethod);
        return methodSymbol?.IsTestMethod(context.Compilation) == true ||
               IsInTestClass(methodSymbol, context.Compilation);
    }

    private static bool IsInTestClass(IMethodSymbol? methodSymbol, Compilation compilation)
    {
        if (methodSymbol?.ContainingType == null)
        {
            return false;
        }

        var testAttributeType = compilation.GetTypeByMetadataName("TUnit.Core.TestAttribute");
        if (testAttributeType == null)
        {
            return false;
        }

        return methodSymbol.ContainingType.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(m => m.GetAttributes()
                .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, testAttributeType)));
    }

    private static bool IsCalledOnTupleType(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
        var type = typeInfo.Type;

        return type != null && IsTupleType(type);
    }

    private static bool IsTupleType(ITypeSymbol type)
    {
        // Check if it's a tuple type using the IsTupleType property
        if (type is INamedTypeSymbol namedType)
        {
            return namedType.IsTupleType;
        }
        return false;
    }

    private static bool IsCalledOnTypeRepresentingTuple(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // Check if the expression is a variable that holds a Type object
        var expressionSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol;
        if (expressionSymbol is not ILocalSymbol localSymbol)
        {
            return false;
        }

        // Check if this variable was assigned from typeof() expression
        var variableDeclarator = localSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
        if (variableDeclarator?.Initializer?.Value is TypeOfExpressionSyntax typeOfExpression)
        {
            // Get the type that typeof() is operating on
            var typeInfo = context.SemanticModel.GetTypeInfo(typeOfExpression.Type, context.CancellationToken);
            return typeInfo.Type != null && IsTupleType(typeInfo.Type);
        }

        return false;
    }

    private static bool IsCustomConversionOperator(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
    {
        // Check if this is a MethodInfo.Invoke call on an op_Implicit or op_Explicit method
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol invokeMethod || invokeMethod.Name != "Invoke")
        {
            return false;
        }

        // Try to find what method is being invoked
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var targetSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken);
            
            // Check if we're in a context that looks like conversion operator usage
            var parentMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (parentMethod != null)
            {
                var methodText = parentMethod.ToFullString();
                return methodText.Contains("op_Implicit") || methodText.Contains("op_Explicit") ||
                       methodText.Contains("GetConversionMethod");
            }
        }

        return false;
    }

    private static bool HasAotCompatibleAttribute(IMethodSymbol method)
    {
        // Check method attributes
        if (method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "AotCompatibleAttribute"))
        {
            return true;
        }

        // Check containing type attributes
        if (method.ContainingType != null &&
            method.ContainingType.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "AotCompatibleAttribute"))
        {
            return true;
        }

        return false;
    }

    private static bool IsDataSourceAttribute(AttributeData attr, Compilation compilation)
    {
        var dataSourceInterface = compilation.GetTypeByMetadataName("TUnit.Core.IDataSourceAttribute");
        if (dataSourceInterface == null || attr.AttributeClass == null)
        {
            return false;
        }

        return attr.AttributeClass.AllInterfaces.Contains(dataSourceInterface, SymbolEqualityComparer.Default) ||
               SymbolEqualityComparer.Default.Equals(attr.AttributeClass, dataSourceInterface);
    }
}