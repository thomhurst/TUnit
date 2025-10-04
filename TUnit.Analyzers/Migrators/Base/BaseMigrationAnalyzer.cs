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
                Flag(context);
                return;
            }

            if (AnalyzeAttributes(context, symbol))
            {
                return;
            }

            foreach (var methodSymbol in symbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (AnalyzeAttributes(context, methodSymbol))
                {
                    return;
                }
            }

            if (CheckUsingDirectives(classDeclarationSyntax))
            {
                Flag(context);
                return;
            }

            if (HasFrameworkTypes(symbol))
            {
                Flag(context);
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

    protected virtual bool CheckUsingDirectives(ClassDeclarationSyntax classDeclarationSyntax)
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
                return true;
            }
        }

        return false;
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

    protected virtual bool AnalyzeAttributes(SyntaxNodeAnalysisContext context, ISymbol symbol)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            var namespaceName = attributeData.AttributeClass?.ContainingNamespace?.Name;
            var fullNamespace = attributeData.AttributeClass?.ContainingNamespace?.ToDisplayString();
            
            if (namespaceName == TargetFrameworkNamespace || IsFrameworkNamespace(fullNamespace))
            {
                Flag(context);
                return true;
            }
        }

        return false;
    }

    protected abstract bool IsFrameworkUsing(string usingName);
    protected abstract bool IsFrameworkNamespace(string? namespaceName);

    protected void Flag(SyntaxNodeAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(DiagnosticRule, context.Node.GetLocation()));
    }
}