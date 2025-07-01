using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

/// <summary>
/// Analyzer that detects patterns incompatible with AOT compilation and provides actionable error messages
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AotCompatibilityAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Rules.GenericTestMissingExplicitInstantiation,
            Rules.DynamicDataSourceNotAotCompatible,
            Rules.OpenGenericTypeNotAotCompatible);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeGenericTestMethods, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeGenericTestClasses, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeDataSourceAttributes, SyntaxKind.Attribute);
    }

    private static void AnalyzeGenericTestMethods(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

        if (methodSymbol == null || !IsTestMethod(methodSymbol))
            return;

        // Skip test methods in abstract classes - they can't be instantiated directly
        // and type inference happens when concrete classes inherit from them
        if (methodSymbol.ContainingType.IsAbstract)
            return;

        // Check for generic test methods without explicit instantiation
        if (methodSymbol.IsGenericMethod)
        {
            var hasGenerateGenericTestAttribute = HasGenerateGenericTestAttribute(methodSymbol) ||
                                                   HasGenerateGenericTestAttribute(methodSymbol.ContainingType);

            if (!hasGenerateGenericTestAttribute)
            {
                // Check if types can be inferred from data sources
                var canInferTypes = CanInferGenericTypes(methodSymbol);
                
                if (!canInferTypes)
                {
                    var diagnostic = Diagnostic.Create(
                        Rules.GenericTestMissingExplicitInstantiation,
                        methodDeclaration.Identifier.GetLocation(),
                        methodSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        // Check for generic containing type without explicit instantiation
        if (methodSymbol.ContainingType.IsGenericType && methodSymbol.ContainingType.TypeParameters.Length > 0)
        {
            var hasGenerateGenericTestAttribute = HasGenerateGenericTestAttribute(methodSymbol.ContainingType);

            if (!hasGenerateGenericTestAttribute)
            {
                // Check if types can be inferred from data sources
                var canInferTypes = CanInferGenericTypes(methodSymbol);
                
                if (!canInferTypes)
                {
                    var diagnostic = Diagnostic.Create(
                        Rules.GenericTestMissingExplicitInstantiation,
                        methodDeclaration.Identifier.GetLocation(),
                        $"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}");

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static void AnalyzeGenericTestClasses(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol == null || !classSymbol.IsGenericType)
            return;

        // Skip abstract classes - they can't be instantiated directly and type inference
        // happens when concrete classes inherit from them with actual data sources
        if (classSymbol.IsAbstract)
            return;

        // Check if this class contains test methods
        var testMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(IsTestMethod)
            .ToList();

        if (!testMethods.Any())
            return;

        // Check for generic test class without explicit instantiation
        if (classSymbol.TypeParameters.Length > 0)
        {
            var hasGenerateGenericTestAttribute = HasGenerateGenericTestAttribute(classSymbol);

            if (!hasGenerateGenericTestAttribute)
            {
                // Check if types can be inferred from any test method's data sources
                var canInferTypes = testMethods.Any(CanInferGenericTypes);
                
                if (!canInferTypes)
                {
                    var diagnostic = Diagnostic.Create(
                        Rules.GenericTestMissingExplicitInstantiation,
                        classDeclaration.Identifier.GetLocation(),
                        classSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }


    private static void AnalyzeDataSourceAttributes(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        var attributeSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;

        if (attributeSymbol?.ContainingType == null)
            return;

        var attributeTypeName = attributeSymbol.ContainingType.Name;

        if (attributeTypeName == "MethodDataSourceAttribute")
        {
            // Analyze MethodDataSource to see if it's AOT-compatible
            if (IsMethodDataSourceDynamic(context, attribute))
            {
                var diagnostic = Diagnostic.Create(
                    Rules.DynamicDataSourceNotAotCompatible,
                    attribute.GetLocation(),
                    attributeTypeName);

                context.ReportDiagnostic(diagnostic);
            }
        }
        else if (attributeTypeName == "ClassDataSourceAttribute")
        {
            // Check if it's the generic version ClassDataSourceAttribute<T> which is AOT-compatible
            if (attributeSymbol.ContainingType.IsGenericType)
            {
                // Generic version inherits from AsyncDataSourceGeneratorAttribute<T> and is AOT-compatible
                return;
            }
            
            // For non-generic version, check if it's using one of the specific constructors (1-5 parameters)
            // which have proper DynamicallyAccessedMembers attributes and are AOT-compatible
            if (attribute.ArgumentList?.Arguments.Count >= 1 && attribute.ArgumentList?.Arguments.Count <= 5)
            {
                // Check if all arguments are typeof() expressions (compile-time constants)
                var allArgumentsAreTypeOf = attribute.ArgumentList.Arguments.All(arg =>
                    arg.Expression is TypeOfExpressionSyntax);
                
                if (allArgumentsAreTypeOf)
                {
                    // Using specific constructors with typeof() - these are AOT-compatible
                    return;
                }
            }
            
            // Using params constructor or dynamic types - not AOT-compatible
            var diagnostic = Diagnostic.Create(
                Rules.DynamicDataSourceNotAotCompatible,
                attribute.GetLocation(),
                attributeTypeName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsTestMethod(IMethodSymbol method)
    {
        return method.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "TestAttribute" ||
            attr.AttributeClass?.Name == "Test");
    }

    private static bool HasGenerateGenericTestAttribute(ISymbol symbol)
    {
        return symbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "GenerateGenericTestAttribute");
    }

    /// <summary>
    /// Checks if generic types can be inferred from test method data sources
    /// </summary>
    private static bool CanInferGenericTypes(IMethodSymbol method)
    {
        // Check for Arguments attributes that could provide type inference
        var hasArgumentsAttributes = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ArgumentsAttribute" || a.AttributeClass?.Name == "Arguments");
        
        if (hasArgumentsAttributes)
        {
            return true; // Arguments attributes can provide type inference
        }
        
        // Check for MethodDataSource attributes
        var hasMethodDataSourceAttributes = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
        
        if (hasMethodDataSourceAttributes)
        {
            return true; // MethodDataSource attributes can provide type inference
        }
        
        // Check for AsyncDataSourceGenerator attributes
        var hasAsyncDataSourceGeneratorAttributes = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name.Contains("AsyncDataSourceGeneratorAttribute") == true);
        
        if (hasAsyncDataSourceGeneratorAttributes)
        {
            return true; // AsyncDataSourceGenerator attributes can provide type inference
        }
        
        return false;
    }

    /// <summary>
    /// Determines if a MethodDataSource attribute uses dynamic resolution patterns
    /// </summary>
    private static bool IsMethodDataSourceDynamic(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
    {
        if (attribute.ArgumentList?.Arguments.Count == 0)
            return true; // No arguments means it can't be statically resolved

        var firstArgument = attribute.ArgumentList?.Arguments[0];
        if (firstArgument == null)
            return true;
        
        // Check if first argument is nameof() - this is AOT-compatible
        if (firstArgument.Expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is IdentifierNameSyntax identifier &&
            identifier.Identifier.ValueText == "nameof")
        {
            return false; // nameof() is AOT-compatible
        }

        // Check if first argument is a string literal - this is AOT-compatible
        if (firstArgument.Expression is LiteralExpressionSyntax literal &&
            literal.Token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralToken))
        {
            return false; // String literal method names are AOT-compatible
        }

        // Check for typeof() with method name - pattern: [MethodDataSource(typeof(SomeType), "MethodName")]
        if (attribute.ArgumentList?.Arguments.Count >= 2)
        {
            var secondArgument = attribute.ArgumentList.Arguments[1];
            
            // If first arg is typeof() and second is string literal or nameof(), it's AOT-compatible
            if (firstArgument.Expression is TypeOfExpressionSyntax &&
                (secondArgument.Expression is LiteralExpressionSyntax secondLiteral &&
                 secondLiteral.Token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralToken) ||
                 secondArgument.Expression is InvocationExpressionSyntax secondInvocation &&
                 secondInvocation.Expression is IdentifierNameSyntax secondIdentifier &&
                 secondIdentifier.Identifier.ValueText == "nameof"))
            {
                return false; // typeof() + string/nameof() is AOT-compatible
            }
        }

        // If we can't determine the pattern, assume it's dynamic
        return true;
    }

}