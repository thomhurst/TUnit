using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace TUnit.Assertions.Analyzers.CodeFixers.Extensions;

public static class DocumentEditorExtensions
{
    public static async Task AddUsingDirective(this DocumentEditor editor, string namespaceName)
    {
        if (await editor.OriginalDocument.NamespaceExists(namespaceName))
        {
            return;
        }
        
        var root = editor.OriginalRoot as CompilationUnitSyntax;
        
        if (root == null)
        {
            return;
        }
    
        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
    
        var newRoot = root.AddUsings(usingDirective);
        
        editor.ReplaceNode(root, newRoot);
    }
}