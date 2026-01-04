using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers.Base;

/// <summary>
/// Transforms method signatures that contain await expressions but are not marked as async.
/// Converts void methods to async Task and T-returning methods to async Task&lt;T&gt;.
/// </summary>
public class AsyncMethodSignatureRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // First, visit children to ensure nested content is processed
        node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

        // Skip if already async or abstract
        if (node.Modifiers.Any(SyntaxKind.AsyncKeyword) ||
            node.Modifiers.Any(SyntaxKind.AbstractKeyword))
        {
            return node;
        }

        // Check if method contains await expressions
        bool hasAwait = node.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
        if (!hasAwait)
        {
            return node;
        }

        // Convert the return type
        var newReturnType = ConvertReturnType(node.ReturnType);

        // Add async modifier after access modifiers but before other modifiers (like static)
        var newModifiers = InsertAsyncModifier(node.Modifiers);

        return node
            .WithReturnType(newReturnType)
            .WithModifiers(newModifiers);
    }

    private static TypeSyntax ConvertReturnType(TypeSyntax returnType)
    {
        // void -> Task
        if (returnType is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            return SyntaxFactory.ParseTypeName("Task")
                .WithLeadingTrivia(returnType.GetLeadingTrivia())
                .WithTrailingTrivia(returnType.GetTrailingTrivia());
        }

        // T -> Task<T>
        var innerType = returnType.WithoutTrivia();
        return SyntaxFactory.GenericName("Task")
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(innerType)))
            .WithLeadingTrivia(returnType.GetLeadingTrivia())
            .WithTrailingTrivia(returnType.GetTrailingTrivia());
    }

    private static SyntaxTokenList InsertAsyncModifier(SyntaxTokenList modifiers)
    {
        // Find the right position for async (after public/private/etc, before static/virtual/etc)
        int insertIndex = 0;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            if (modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword))
            {
                insertIndex = i + 1;
            }
        }

        var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        return modifiers.Insert(insertIndex, asyncModifier);
    }
}
