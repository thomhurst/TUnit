using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

/// <summary>
/// Transforms NUnit [TestCase(..., ExpectedResult = X)] patterns to TUnit assertions.
/// </summary>
public class NUnitExpectedResultRewriter : CSharpSyntaxRewriter
{
    // Kept for consistency with other rewriters and potential future semantic analysis needs
    private readonly SemanticModel _semanticModel;

    public NUnitExpectedResultRewriter(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Check if method has any TestCase attributes with ExpectedResult
        var testCaseAttributes = GetTestCaseAttributesWithExpectedResult(node);

        if (testCaseAttributes.Count == 0)
        {
            return base.VisitMethodDeclaration(node);
        }

        // Get the return type (will become the expected parameter type)
        var returnType = node.ReturnType;
        if (returnType is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            // void methods can't have ExpectedResult
            return base.VisitMethodDeclaration(node);
        }

        // Transform the method
        return TransformMethod(node, testCaseAttributes, returnType);
    }

    private List<AttributeSyntax> GetTestCaseAttributesWithExpectedResult(MethodDeclarationSyntax method)
    {
        var result = new List<AttributeSyntax>();

        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name is "TestCase" or "NUnit.Framework.TestCase" or "TestCaseAttribute" or "NUnit.Framework.TestCaseAttribute")
                {
                    if (HasExpectedResultArgument(attribute))
                    {
                        result.Add(attribute);
                    }
                }
            }
        }

        return result;
    }

    private bool HasExpectedResultArgument(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList == null)
        {
            return false;
        }

        foreach (var arg in attribute.ArgumentList.Arguments)
        {
            if (arg.NameEquals?.Name.Identifier.Text == "ExpectedResult")
            {
                return true;
            }
        }
        return false;
    }

    private MethodDeclarationSyntax TransformMethod(
        MethodDeclarationSyntax method,
        List<AttributeSyntax> testCaseAttributes,
        TypeSyntax originalReturnType)
    {
        // 1. Add 'expected' parameter
        var expectedParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier("expected"))
            .WithType(originalReturnType.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space));

        var newParameters = method.ParameterList.AddParameters(expectedParam);

        // 2. Transform the body
        var newBody = TransformBody(method, originalReturnType);

        // 3. Build the new method with async Task return type
        var asyncTaskType = SyntaxFactory.ParseTypeName("Task").WithTrailingTrivia(SyntaxFactory.Space);

        var newMethod = method
            .WithReturnType(asyncTaskType)
            .WithParameterList(newParameters)
            .WithBody(newBody)
            .WithExpressionBody(null)
            .WithSemicolonToken(default);

        // 4. Add async modifier if not present
        if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            var modifiers = method.Modifiers;
            var insertIndex = 0;

            // Find the right position for async (after public/private/etc, before static)
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].IsKind(SyntaxKind.PublicKeyword) ||
                    modifiers[i].IsKind(SyntaxKind.PrivateKeyword) ||
                    modifiers[i].IsKind(SyntaxKind.ProtectedKeyword) ||
                    modifiers[i].IsKind(SyntaxKind.InternalKeyword))
                {
                    insertIndex = i + 1;
                }
            }

            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);
            modifiers = modifiers.Insert(insertIndex, asyncModifier);
            newMethod = newMethod.WithModifiers(modifiers);
        }

        // 5. Update attribute lists (remove ExpectedResult from TestCase, add [Test])
        var newAttributeLists = TransformAttributeLists(method.AttributeLists, testCaseAttributes);
        newMethod = newMethod.WithAttributeLists(newAttributeLists);

        return newMethod;
    }

    private BlockSyntax TransformBody(MethodDeclarationSyntax method, TypeSyntax returnType)
    {
        ExpressionSyntax returnExpression;

        if (method.ExpressionBody != null)
        {
            // Expression-bodied: => a + b
            returnExpression = method.ExpressionBody.Expression;
        }
        else if (method.Body != null)
        {
            // Block body - find return statements
            var returnStatements = new List<ReturnStatementSyntax>();
            foreach (var statement in method.Body.Statements)
            {
                if (statement is ReturnStatementSyntax returnStmt)
                {
                    returnStatements.Add(returnStmt);
                }
            }

            if (returnStatements.Count == 1 && returnStatements[0].Expression != null)
            {
                // Single return - use the expression directly
                returnExpression = returnStatements[0].Expression;

                // Build new body with all statements except the return, plus assertion
                var statementsWithoutReturn = new List<StatementSyntax>();
                foreach (var s in method.Body.Statements)
                {
                    if (s != returnStatements[0])
                    {
                        statementsWithoutReturn.Add(s);
                    }
                }

                var assertStatement = CreateAssertStatement(returnExpression);
                statementsWithoutReturn.Add(assertStatement);

                return SyntaxFactory.Block(statementsWithoutReturn);
            }
            else if (returnStatements.Count > 1)
            {
                // Multiple returns - use local variable pattern
                return TransformMultipleReturns(method.Body, returnType);
            }
            else
            {
                // No return found - shouldn't happen for ExpectedResult
                return method.Body;
            }
        }
        else
        {
            // No body - shouldn't happen
            return SyntaxFactory.Block();
        }

        // For expression-bodied, create block with assertion
        var assertion = CreateAssertStatement(returnExpression);
        return SyntaxFactory.Block(assertion);
    }

    private BlockSyntax TransformMultipleReturns(BlockSyntax body, TypeSyntax returnType)
    {
        // Declare: {returnType} result;
        var resultDeclaration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(returnType.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator("result"))));

        // Replace each 'return X;' with 'result = X;'
        var rewriter = new ReturnToAssignmentRewriter();
        var transformedBody = (BlockSyntax)rewriter.Visit(body);

        // Add result declaration at start
        var statements = new List<StatementSyntax> { resultDeclaration };
        statements.AddRange(transformedBody.Statements);

        // Add assertion at end
        var assertStatement = CreateAssertStatement(
            SyntaxFactory.IdentifierName("result"));
        statements.Add(assertStatement);

        return SyntaxFactory.Block(statements);
    }

    private ExpressionStatementSyntax CreateAssertStatement(ExpressionSyntax actualExpression)
    {
        // await Assert.That(actualExpression).IsEqualTo(expected);
        var assertThat = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("That")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(actualExpression))));

        var isEqualTo = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                assertThat,
                SyntaxFactory.IdentifierName("IsEqualTo")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("expected")))));

        var awaitExpr = SyntaxFactory.AwaitExpression(isEqualTo);

        return SyntaxFactory.ExpressionStatement(awaitExpr);
    }

    private SyntaxList<AttributeListSyntax> TransformAttributeLists(
        SyntaxList<AttributeListSyntax> attributeLists,
        List<AttributeSyntax> testCaseAttributes)
    {
        var result = new List<AttributeListSyntax>();
        bool hasTestAttribute = false;

        foreach (var attrList in attributeLists)
        {
            var newAttributes = new List<AttributeSyntax>();

            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();

                if (name is "Test" or "NUnit.Framework.Test")
                {
                    hasTestAttribute = true;
                    newAttributes.Add(attr);
                }
                else if (testCaseAttributes.Contains(attr))
                {
                    // Transform TestCase with ExpectedResult
                    var transformed = TransformTestCaseAttribute(attr);
                    newAttributes.Add(transformed);
                }
                else
                {
                    newAttributes.Add(attr);
                }
            }

            if (newAttributes.Count > 0)
            {
                result.Add(attrList.WithAttributes(SyntaxFactory.SeparatedList(newAttributes)));
            }
        }

        // Add [Test] attribute if not present
        if (!hasTestAttribute && result.Count > 0)
        {
            var testAttr = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test"));
            var testAttrList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(testAttr))
                .WithLeadingTrivia(result[0].GetLeadingTrivia())
                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));
            result.Insert(0, testAttrList);
        }

        return SyntaxFactory.List(result);
    }

    private AttributeSyntax TransformTestCaseAttribute(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList == null)
        {
            return attribute;
        }

        var newArgs = new List<AttributeArgumentSyntax>();
        ExpressionSyntax? expectedValue = null;

        foreach (var arg in attribute.ArgumentList.Arguments)
        {
            if (arg.NameEquals?.Name.Identifier.Text == "ExpectedResult")
            {
                expectedValue = arg.Expression;
            }
            else if (arg.NameColon == null && arg.NameEquals == null)
            {
                // Positional argument - keep it
                newArgs.Add(arg);
            }
            // Skip other named arguments for now
        }

        // Add expected value as last positional argument
        if (expectedValue != null)
        {
            newArgs.Add(SyntaxFactory.AttributeArgument(expectedValue));
        }

        // The attribute will be renamed to "Arguments" by the existing attribute rewriter
        return attribute.WithArgumentList(
            SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(newArgs)));
    }

    private class ReturnToAssignmentRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (node.Expression == null)
            {
                return node;
            }

            // return X; -> result = X;
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("result"),
                    node.Expression));
        }
    }
}
