using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

/// <summary>
/// Extracts NUnit TestCase properties and converts them to TUnit attributes.
/// Maps: Description/Author → Property, Explicit → Explicit
/// Note: TestName → DisplayName and Category → Categories are now handled inline on [Arguments]
/// by NUnitAttributeRewriter, so we don't generate separate attributes for those.
/// </summary>
public class NUnitTestCasePropertyRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Get all TestCase attributes on this method
        var testCaseAttributes = GetTestCaseAttributes(node);

        if (testCaseAttributes.Count == 0)
        {
            return base.VisitMethodDeclaration(node);
        }

        // Extract properties from all TestCase attributes
        var properties = ExtractProperties(testCaseAttributes);

        // Generate new attribute lists for the extracted properties
        var newAttributeLists = GeneratePropertyAttributes(properties, node.AttributeLists);

        // If we generated new attributes, add them to the method
        if (newAttributeLists.Count > node.AttributeLists.Count)
        {
            return node.WithAttributeLists(newAttributeLists);
        }

        return base.VisitMethodDeclaration(node);
    }

    private List<AttributeSyntax> GetTestCaseAttributes(MethodDeclarationSyntax method)
    {
        var result = new List<AttributeSyntax>();

        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name is "TestCase" or "NUnit.Framework.TestCase" or "TestCaseAttribute" or "NUnit.Framework.TestCaseAttribute")
                {
                    result.Add(attribute);
                }
            }
        }

        return result;
    }

    private TestCaseProperties ExtractProperties(List<AttributeSyntax> testCaseAttributes)
    {
        // Early return if no TestCase has named arguments (performance optimization)
        bool hasNamedArguments = false;
        foreach (var attribute in testCaseAttributes)
        {
            if (attribute.ArgumentList?.Arguments.Any(a => a.NameEquals != null) == true)
            {
                hasNamedArguments = true;
                break;
            }
        }

        if (!hasNamedArguments)
        {
            return new TestCaseProperties();
        }

        var properties = new TestCaseProperties();

        foreach (var attribute in testCaseAttributes)
        {
            if (attribute.ArgumentList == null)
            {
                continue;
            }

            foreach (var arg in attribute.ArgumentList.Arguments)
            {
                var propertyName = arg.NameEquals?.Name.Identifier.Text;

                switch (propertyName)
                {
                    // Note: TestName and Category are now handled inline on [Arguments] by NUnitAttributeRewriter
                    // so we don't need to extract them here anymore.

                    case "Description":
                        var descValue = GetStringValue(arg.Expression);
                        if (descValue != null)
                        {
                            properties.Descriptions.Add(descValue);
                        }
                        break;

                    case "Author":
                        var authorValue = GetStringValue(arg.Expression);
                        if (authorValue != null)
                        {
                            properties.Authors.Add(authorValue);
                        }
                        break;

                    case "Explicit":
                        if (arg.Expression is LiteralExpressionSyntax literal &&
                            literal.IsKind(SyntaxKind.TrueLiteralExpression))
                        {
                            properties.IsExplicit = true;
                        }
                        break;

                    case "ExplicitReason":
                        var explicitReason = GetStringValue(arg.Expression);
                        if (explicitReason != null)
                        {
                            properties.ExplicitReasons.Add(explicitReason);
                            properties.IsExplicit = true;
                        }
                        break;
                }
            }
        }

        return properties;
    }

    private string? GetStringValue(ExpressionSyntax expression)
    {
        // Only handle string literals - interpolated strings, concatenation, and const references
        // cannot be reliably migrated and should be handled manually by the user
        if (expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    private SyntaxList<AttributeListSyntax> GeneratePropertyAttributes(
        TestCaseProperties properties,
        SyntaxList<AttributeListSyntax> existingAttributeLists)
    {
        var newLists = new List<AttributeListSyntax>(existingAttributeLists);
        var leadingTrivia = existingAttributeLists.Count > 0
            ? existingAttributeLists[0].GetLeadingTrivia()
            : SyntaxTriviaList.Empty;

        // Get indentation from existing attributes
        var indentation = GetIndentation(leadingTrivia);

        // Note: TestName → DisplayName and Category → Categories are now handled inline on [Arguments]
        // by NUnitAttributeRewriter.ConvertTestCaseArguments, so we don't generate separate attributes here.

        // Description - use Property attribute
        if (properties.Descriptions.Count > 0)
        {
            var description = properties.Descriptions.First();
            var propAttr = CreatePropertyAttribute("Description", description);
            newLists.Add(CreateAttributeList(propAttr, indentation));
        }

        // Author - use Property attribute
        if (properties.Authors.Count > 0)
        {
            var author = properties.Authors.First();
            var propAttr = CreatePropertyAttribute("Author", author);
            newLists.Add(CreateAttributeList(propAttr, indentation));
        }

        // Explicit
        if (properties.IsExplicit)
        {
            var explicitAttr = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Explicit"));
            newLists.Add(CreateAttributeList(explicitAttr, indentation));

            // If there's an explicit reason, add it as a Property
            if (properties.ExplicitReasons.Count > 0)
            {
                var reason = properties.ExplicitReasons.First();
                var propAttr = CreatePropertyAttribute("ExplicitReason", reason);
                newLists.Add(CreateAttributeList(propAttr, indentation));
            }
        }

        return SyntaxFactory.List(newLists);
    }

    private AttributeSyntax CreatePropertyAttribute(string name, string value)
    {
        return SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("Property"),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(name))),
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(value)))
                })));
    }

    private AttributeListSyntax CreateAttributeList(AttributeSyntax attribute, SyntaxTrivia indentation)
    {
        return SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(attribute))
            .WithLeadingTrivia(indentation)
            .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));
    }

    private SyntaxTrivia GetIndentation(SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                return trivia;
            }
        }

        return SyntaxFactory.Whitespace("    ");
    }

    private class TestCaseProperties
    {
        // Note: TestNames and Categories are now handled inline on [Arguments] by NUnitAttributeRewriter
        public HashSet<string> Descriptions { get; } = new();
        public HashSet<string> Authors { get; } = new();
        public bool IsExplicit { get; set; }
        public HashSet<string> ExplicitReasons { get; } = new();
    }
}
