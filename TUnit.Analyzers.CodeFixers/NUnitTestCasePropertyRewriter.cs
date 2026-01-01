using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

/// <summary>
/// Extracts NUnit TestCase properties and converts them to TUnit attributes.
/// Maps: TestName → DisplayName, Category → Category, Description/Author → Property, Explicit → Explicit
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
                    case "TestName":
                        var testNameValue = GetStringValue(arg.Expression);
                        if (testNameValue != null)
                        {
                            properties.TestNames.Add(testNameValue);
                        }
                        break;

                    case "Category":
                        var categoryValue = GetStringValue(arg.Expression);
                        if (categoryValue != null)
                        {
                            properties.Categories.Add(categoryValue);
                        }
                        break;

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
        if (expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        // Handle interpolated strings or other expressions by getting the text
        return expression.ToString().Trim('"');
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

        // DisplayName from TestName (use first if multiple, or try to create pattern)
        if (properties.TestNames.Count > 0)
        {
            var displayNameAttr = CreateDisplayNameAttribute(properties.TestNames);
            if (displayNameAttr != null)
            {
                newLists.Add(CreateAttributeList(displayNameAttr, indentation));
            }
        }

        // Category - add all unique categories
        foreach (var category in properties.Categories.Distinct())
        {
            var categoryAttr = SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("Category"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(category))))));
            newLists.Add(CreateAttributeList(categoryAttr, indentation));
        }

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

    private AttributeSyntax? CreateDisplayNameAttribute(HashSet<string> testNames)
    {
        if (testNames.Count == 0)
        {
            return null;
        }

        // If only one test name, use it directly
        // If multiple different names, use the first one (user can manually adjust)
        var displayName = testNames.First();

        return SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("DisplayName"),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(displayName))))));
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
        public HashSet<string> TestNames { get; } = new();
        public HashSet<string> Categories { get; } = new();
        public HashSet<string> Descriptions { get; } = new();
        public HashSet<string> Authors { get; } = new();
        public bool IsExplicit { get; set; }
        public HashSet<string> ExplicitReasons { get; } = new();
    }
}
