using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

            if (HasFrameworkInterfaces(symbol))
            {
                Flag(context, classDeclarationSyntax.GetLocation());
                return;
            }

            var classAttributeLocation = AnalyzeAttributes(context, symbol, classDeclarationSyntax);
            if (classAttributeLocation != null)
            {
                Flag(context, classAttributeLocation);
                return;
            }

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
                    Flag(context, methodAttributeLocation);
                    return;
                }
            }

            var usingLocation = CheckUsingDirectives(classDeclarationSyntax);
            if (usingLocation != null)
            {
                Flag(context, usingLocation);
                return;
            }

            if (HasFrameworkTypes(symbol))
            {
                Flag(context, classDeclarationSyntax.GetLocation());
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

    protected virtual bool HasFrameworkTypes(INamedTypeSymbol namedTypeSymbol)
    {
        var members = namedTypeSymbol.GetMembers();

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

        return types.Any();
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