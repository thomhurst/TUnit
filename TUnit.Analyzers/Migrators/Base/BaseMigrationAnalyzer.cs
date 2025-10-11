using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Analyzers.Migrators.Base;

public abstract class BaseMigrationAnalyzer : ConcurrentDiagnosticAnalyzer
{
    protected abstract string TargetFrameworkNamespace { get; }
    protected abstract DiagnosticDescriptor DiagnosticRule { get; }
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(DiagnosticRule);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.CompilationUnit);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not CompilationUnitSyntax compilationUnitSyntax)
        {
            return;
        }

        var classDeclarationSyntaxes = compilationUnitSyntax
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        foreach (var classDeclarationSyntax in classDeclarationSyntaxes)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (symbol is null)
            {
                return;
            }

            // Priority 1: Framework interfaces on the class
            if (HasFrameworkInterfaces(symbol))
            {
                Flag(context, classDeclarationSyntax.GetLocation());
                return;
            }

            // Priority 2: Framework attributes on the class
            var classAttributeLocation = AnalyzeAttributes(context, symbol, classDeclarationSyntax);
            if (classAttributeLocation != null)
            {
                Flag(context, classAttributeLocation);
                return;
            }

            // Priority 3: Framework types in the class (e.g., Assert calls)
            // Flag on the class declaration so the entire class can be migrated
            if (HasFrameworkTypes(context, symbol, classDeclarationSyntax))
            {
                // Flag the class declaration line: from start of modifiers to end of class name
                var classHeaderSpan = TextSpan.FromBounds(
                    classDeclarationSyntax.SpanStart,
                    classDeclarationSyntax.Identifier.Span.End);
                var classHeaderLocation = Location.Create(
                    classDeclarationSyntax.SyntaxTree,
                    classHeaderSpan);
                Flag(context, classHeaderLocation);
                return;
            }

            // Priority 4: Framework attributes on methods
            // Collect all methods with framework attributes
            var methodsWithFrameworkAttributes = new List<(IMethodSymbol symbol, Location location)>();
            foreach (var methodSymbol in symbol.GetMembers().OfType<IMethodSymbol>())
            {
                var syntaxReferences = methodSymbol.DeclaringSyntaxReferences;
                if (syntaxReferences.Length == 0)
                {
                    continue;
                }

                var methodSyntax = syntaxReferences[0].GetSyntax();
                var methodAttributeLocation = AnalyzeAttributes(context, methodSymbol, methodSyntax);
                if (methodAttributeLocation != null)
                {
                    methodsWithFrameworkAttributes.Add((methodSymbol, methodAttributeLocation));
                }
            }

            // If multiple methods have framework attributes, flag the class declaration
            // If only one method has framework attributes, flag that specific attribute
            if (methodsWithFrameworkAttributes.Count > 1)
            {
                var classHeaderSpan = TextSpan.FromBounds(
                    classDeclarationSyntax.SpanStart,
                    classDeclarationSyntax.Identifier.Span.End);
                var classHeaderLocation = Location.Create(
                    classDeclarationSyntax.SyntaxTree,
                    classHeaderSpan);
                Flag(context, classHeaderLocation);
                return;
            }
            else if (methodsWithFrameworkAttributes.Count == 1)
            {
                Flag(context, methodsWithFrameworkAttributes[0].location);
                return;
            }

            // Priority 5 (lowest): Using directives - only flag if nothing else was found
            var usingLocation = CheckUsingDirectives(classDeclarationSyntax);
            if (usingLocation != null)
            {
                Flag(context, usingLocation);
                return;
            }
        }
    }

    protected virtual bool HasFrameworkInterfaces(INamedTypeSymbol symbol)
    {
        return symbol.AllInterfaces.Any(i => 
            i.ContainingNamespace?.Name.StartsWith(TargetFrameworkNamespace) is true ||
            IsFrameworkNamespace(i.ContainingNamespace?.ToDisplayString()));
    }

    protected virtual Location? CheckUsingDirectives(ClassDeclarationSyntax classDeclarationSyntax)
    {
        var usingDirectiveSyntaxes = classDeclarationSyntax
            .SyntaxTree
            .GetCompilationUnitRoot()
            .Usings;

        foreach (var usingDirectiveSyntax in usingDirectiveSyntaxes)
        {
            var nameString = usingDirectiveSyntax.Name?.ToString() ?? "";
            if (IsFrameworkUsing(nameString))
            {
                return usingDirectiveSyntax.GetLocation();
            }
        }

        return null;
    }

    protected virtual bool HasFrameworkTypes(SyntaxNodeAnalysisContext context, INamedTypeSymbol namedTypeSymbol, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var members = namedTypeSymbol.GetMembers();

        // Check properties, return types, and fields
        var types = members.OfType<IPropertySymbol>()
                .Where(x => IsFrameworkType(x.Type))
                .Select(x => x.Type)
            .Concat(members.OfType<IMethodSymbol>()
                .Where(x => IsFrameworkType(x.ReturnType))
                .Select(x => x.ReturnType))
            .Concat(members.OfType<IFieldSymbol>()
                .Where(x => IsFrameworkType(x.Type))
                .Select(x => x.Type))
            .ToArray();

        if (types.Any())
        {
            return true;
        }

        // Check for framework type usage in method bodies (e.g., Assert.AreEqual(), CollectionAssert.Contains())
        var invocationExpressions = classDeclarationSyntax
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocationExpressions)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                // Check if the method belongs to a framework type
                if (IsFrameworkType(methodSymbol.ContainingType))
                {
                    return true;
                }
            }
            else if (symbolInfo.Symbol == null)
            {
                // Fallback: if symbol resolution fails completely, check the syntax directly
                // This handles cases where the semantic model hasn't fully resolved types
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var typeExpression = memberAccess.Expression.ToString();
                    if (IsFrameworkTypeName(typeExpression))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    protected virtual bool IsFrameworkTypeName(string typeName)
    {
        // Override in derived classes to provide framework-specific type names
        return false;
    }

    protected virtual bool IsFrameworkType(ITypeSymbol type)
    {
        return type.ContainingNamespace?.Name.StartsWith(TargetFrameworkNamespace) is true ||
               IsFrameworkNamespace(type.ContainingNamespace?.ToDisplayString());
    }

    protected virtual Location? AnalyzeAttributes(SyntaxNodeAnalysisContext context, ISymbol symbol, SyntaxNode syntaxNode)
    {
        var attributes = symbol.GetAttributes();

        for (var i = 0; i < attributes.Length; i++)
        {
            var attributeData = attributes[i];
            var namespaceName = attributeData.AttributeClass?.ContainingNamespace?.Name;
            var fullNamespace = attributeData.AttributeClass?.ContainingNamespace?.ToDisplayString();

            if (namespaceName == TargetFrameworkNamespace || IsFrameworkNamespace(fullNamespace))
            {
                // Get the attribute syntax for this specific attribute
                var attributeSyntax = attributeData.ApplicationSyntaxReference?.GetSyntax();
                if (attributeSyntax != null)
                {
                    // Get the parent AttributeListSyntax to include the brackets [...]
                    if (attributeSyntax.Parent is AttributeListSyntax attributeListSyntax)
                    {
                        // Return the location including brackets but excluding leading trivia
                        return Location.Create(
                            attributeListSyntax.SyntaxTree,
                            attributeListSyntax.Span);
                    }
                    return attributeSyntax.GetLocation();
                }
            }
        }

        return null;
    }

    protected abstract bool IsFrameworkUsing(string usingName);
    protected abstract bool IsFrameworkNamespace(string? namespaceName);

    protected void Flag(SyntaxNodeAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(DiagnosticRule, location));
    }
}