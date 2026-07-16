using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

/// <summary>
/// Extracts NUnit TestCase and Test properties and converts them to TUnit attributes.
/// Maps: Description/Author -> Property, Explicit -> Explicit
/// Note: TestName -> DisplayName and Category -> Categories are now handled inline on [Arguments]
/// by NUnitAttributeRewriter, so we don't generate separate attributes for those.
/// </summary>
public class NUnitTestCasePropertyRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Get all TestCase and Test attributes on this method
        var testCaseAttributes = GetTestCaseAttributes(node);
        var testAttributes = GetTestAttributes(node);

        if (testCaseAttributes.Count == 0 && testAttributes.Count == 0)
        {
            return base.VisitMethodDeclaration(node);
        }

        // Extract properties from all TestCase and Test attributes
        var properties = ExtractProperties(testCaseAttributes);
        var testProperties = ExtractTestProperties(testAttributes);

        // Merge Test properties into TestCase properties (Test properties take precedence if both exist)
        MergeProperties(properties, testProperties);

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

    private List<AttributeSyntax> GetTestAttributes(MethodDeclarationSyntax method)
    {
        var result = new List<AttributeSyntax>();

        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name is "Test" or "NUnit.Framework.Test" or "TestAttribute" or "NUnit.Framework.TestAttribute"
                    or "Theory" or "NUnit.Framework.Theory" or "TheoryAttribute" or "NUnit.Framework.TheoryAttribute")
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

    private TestCaseProperties ExtractTestProperties(List<AttributeSyntax> testAttributes)
    {
        // Extract Description, Author, etc. from [Test] attributes
        // NUnit's [Test] supports: Description, Author, ExpectedResult, TestOf
        var properties = new TestCaseProperties();

        foreach (var attribute in testAttributes)
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

                    // ExpectedResult on [Test] is used with return values, but TUnit doesn't support this
                    // Just skip it - the user will need to handle this manually or use assertions
                    case "ExpectedResult":
                        break;

                    // TestOf specifies the type being tested - convert to Property
                    case "TestOf":
                        // TestOf is a Type, not a string, so we need to handle it differently
                        // For now, skip it as it's rarely used
                        break;
                }
            }
        }

        return properties;
    }

    private void MergeProperties(TestCaseProperties target, TestCaseProperties source)
    {
        // Merge source properties into target
        foreach (var desc in source.Descriptions)
        {
            target.Descriptions.Add(desc);
        }
        foreach (var author in source.Authors)
        {
            target.Authors.Add(author);
        }
        if (source.IsExplicit)
        {
            target.IsExplicit = true;
        }
        foreach (var reason in source.ExplicitReasons)
        {
            target.ExplicitReasons.Add(reason);
        }
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

        // Note: TestName -> DisplayName and Category -> Categories are now handled inline on [Arguments]
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
