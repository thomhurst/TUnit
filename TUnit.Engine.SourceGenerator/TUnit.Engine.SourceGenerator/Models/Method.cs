using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Engine.SourceGenerator.Models;

internal record Method
{
    public MethodDeclarationSyntax MethodDeclarationSyntax { get; }
    public IMethodSymbol MethodSymbol { get; }

    public Method(MethodDeclarationSyntax methodDeclarationSyntax, IMethodSymbol methodSymbol)
    {
        MethodDeclarationSyntax = methodDeclarationSyntax;
        MethodSymbol = methodSymbol;
    }
}