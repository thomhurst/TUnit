using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers.Base;

/// <summary>
/// Ensures that methods with data attributes (like [Arguments]) also have a [Test] attribute.
/// This handles the case where NUnit allows [TestCase] alone, but TUnit requires [Test] + [Arguments].
/// </summary>
public class TestAttributeEnsurer : CSharpSyntaxRewriter
{
    private static readonly string[] DataAttributeNames =
    [
        "Arguments",
        "MethodDataSource",
        "ClassDataSource",
        "MatrixDataSource"
    ];

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // First, visit children
        node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

        // Check if method has any data attributes
        bool hasDataAttribute = HasAnyDataAttribute(node);
        if (!hasDataAttribute)
        {
            return node;
        }

        // Check if method already has [Test] attribute
        bool hasTestAttribute = HasTestAttribute(node);
        if (hasTestAttribute)
        {
            return node;
        }

        // Add [Test] attribute before the first attribute list
        return AddTestAttribute(node);
    }

    private static bool HasAnyDataAttribute(MethodDeclarationSyntax method)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = GetAttributeName(attribute);
                if (DataAttributeNames.Contains(name))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool HasTestAttribute(MethodDeclarationSyntax method)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = GetAttributeName(attribute);
                if (name == "Test")
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static string GetAttributeName(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();

        // Remove "Attribute" suffix if present
        if (name.EndsWith("Attribute"))
        {
            name = name[..^9];
        }

        // Handle fully qualified names (take the last part)
        var lastDot = name.LastIndexOf('.');
        if (lastDot >= 0)
        {
            name = name[(lastDot + 1)..];
        }

        return name;
    }

    private static MethodDeclarationSyntax AddTestAttribute(MethodDeclarationSyntax method)
    {
        if (method.AttributeLists.Count == 0)
        {
            // No existing attributes - add [Test] with proper formatting
            var testAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test"));
            var testAttributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(testAttribute))
                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

            return method.WithAttributeLists(
                SyntaxFactory.SingletonList(testAttributeList));
        }

        // Get the leading trivia (indentation) from the first attribute list
        var firstAttributeList = method.AttributeLists[0];
        var leadingTrivia = firstAttributeList.GetLeadingTrivia();

        // Create [Test] attribute list with same indentation
        var testAttr = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test"));
        var newTestAttrList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(testAttr))
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

        // Strip newlines from first attribute's leading trivia, keep only indentation
        // This prevents double newlines when we insert [Test] before it
        var strippedTrivia = firstAttributeList.GetLeadingTrivia()
            .Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));
        var updatedFirstAttr = firstAttributeList.WithLeadingTrivia(strippedTrivia);

        // Build new attribute list: [Test], then updated first attr (with stripped trivia), then rest
        var newAttributeLists = new SyntaxList<AttributeListSyntax>()
            .Add(newTestAttrList)
            .Add(updatedFirstAttr)
            .AddRange(method.AttributeLists.Skip(1));

        return method.WithAttributeLists(newAttributeLists);
    }
}
