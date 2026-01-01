using System.Linq;
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
        bool hasAsyncModifier = false;
        foreach (var modifier in method.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.AsyncKeyword))
            {
                hasAsyncModifier = true;
                break;
            }
        }

        if (!hasAsyncModifier)
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
            // Block body - find ALL return statements recursively
            var returnStatements = FindAllReturnStatements(method.Body);

            if (returnStatements.Count == 1 && returnStatements[0].Expression != null)
            {
                // Single return - check if it's a simple direct return or nested
                bool isDirectReturn = method.Body.Statements.Contains(returnStatements[0]);

                if (isDirectReturn)
                {
                    // Direct return - use the expression directly
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
                else
                {
                    // Nested return - treat as multiple returns
                    return TransformMultipleReturns(method.Body, returnType);
                }
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

    private List<ReturnStatementSyntax> FindAllReturnStatements(SyntaxNode node)
    {
        var returnStatements = new List<ReturnStatementSyntax>();

        foreach (var descendant in node.DescendantNodes())
        {
            if (descendant is ReturnStatementSyntax returnStmt)
            {
                returnStatements.Add(returnStmt);
            }
        }

        return returnStatements;
    }

    private BlockSyntax TransformMultipleReturns(BlockSyntax body, TypeSyntax returnType)
    {
        // Declare: {returnType} result;
        var resultDeclaration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(returnType.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator("result"))));

        // Transform the statements to use if-else chains
        var transformedStatements = TransformStatementsWithElseChain(body.Statements);

        // Add result declaration at start
        var statements = new List<StatementSyntax> { resultDeclaration };
        statements.AddRange(transformedStatements);

        // Add assertion at end
        var assertStatement = CreateAssertStatement(
            SyntaxFactory.IdentifierName("result"));
        statements.Add(assertStatement);

        return SyntaxFactory.Block(statements);
    }

    private List<StatementSyntax> TransformStatementsWithElseChain(SyntaxList<StatementSyntax> originalStatements)
    {
        var result = new List<StatementSyntax>();
        var ifChainParts = new List<(ExpressionSyntax condition, StatementSyntax statement)>();
        StatementSyntax? elseStatement = null;

        // First pass: collect all if statements with returns and the final return
        for (int i = 0; i < originalStatements.Count; i++)
        {
            var stmt = originalStatements[i];

            if (stmt is IfStatementSyntax ifStmt && ContainsReturn(ifStmt))
            {
                // Extract the condition and the assignment
                var transformedIf = TransformIfStatement(ifStmt);

                // If the statement is a block with a single statement, unwrap it
                var statement = transformedIf.Statement;
                if (statement is BlockSyntax block && block.Statements.Count == 1)
                {
                    statement = block.Statements[0];
                }

                ifChainParts.Add((transformedIf.Condition, statement));
            }
            else if (stmt is ReturnStatementSyntax returnStmt && returnStmt.Expression != null)
            {
                // Final return statement - will become the else clause
                elseStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName("result"),
                        returnStmt.Expression));
            }
            else
            {
                // Non-return statement - add any accumulated if chain first
                if (ifChainParts.Count > 0 || elseStatement != null)
                {
                    result.Add(BuildIfElseChain(ifChainParts, elseStatement));
                    ifChainParts.Clear();
                    elseStatement = null;
                }
                result.Add(stmt);
            }
        }

        // Add any remaining if chain
        if (ifChainParts.Count > 0 || elseStatement != null)
        {
            result.Add(BuildIfElseChain(ifChainParts, elseStatement));
        }

        return result;
    }

    private IfStatementSyntax BuildIfElseChain(
        List<(ExpressionSyntax condition, StatementSyntax statement)> ifChainParts,
        StatementSyntax? elseStatement)
    {
        // Build the chain from the end backwards
        ElseClauseSyntax? elseClause = elseStatement != null
            ? SyntaxFactory.ElseClause(elseStatement.WithoutTrivia())
            : null;

        // Build the if-else chain backwards
        for (int i = ifChainParts.Count - 1; i >= 0; i--)
        {
            var (condition, statement) = ifChainParts[i];

            var ifStmt = SyntaxFactory.IfStatement(
                condition.WithoutTrivia(),
                statement.WithoutTrivia());

            if (elseClause != null)
            {
                ifStmt = ifStmt.WithElse(elseClause);
            }

            // This becomes the else clause for the next level up
            if (i > 0)
            {
                elseClause = SyntaxFactory.ElseClause(ifStmt);
            }
            else
            {
                // This is the root
                return ifStmt;
            }
        }

        // Shouldn't get here if ifChainParts is not empty
        return SyntaxFactory.IfStatement(
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
            SyntaxFactory.Block());
    }

    private bool ContainsReturn(SyntaxNode node)
    {
        return node.DescendantNodes().OfType<ReturnStatementSyntax>().Any();
    }

    private IfStatementSyntax TransformIfStatement(IfStatementSyntax ifStmt)
    {
        // Transform the statement inside the if from 'return X;' to 'result = X;'
        var rewriter = new ReturnToAssignmentRewriter();
        return (IfStatementSyntax)rewriter.Visit(ifStmt);
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
            var unsupportedPropertiesList = new List<string>();

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
                    var (transformed, unsupportedProperties) = TransformTestCaseAttribute(attr);
                    newAttributes.Add(transformed);
                    unsupportedPropertiesList.AddRange(unsupportedProperties);
                }
                else
                {
                    newAttributes.Add(attr);
                }
            }

            if (newAttributes.Count > 0)
            {
                var newAttrList = attrList.WithAttributes(SyntaxFactory.SeparatedList(newAttributes));

                // Add TODO comment as trailing trivia on the attribute list
                if (unsupportedPropertiesList.Count > 0)
                {
                    var todoComment = SyntaxFactory.Comment($" // TODO: TUnit migration - unsupported: {string.Join(", ", unsupportedPropertiesList)}");
                    newAttrList = newAttrList.WithTrailingTrivia(newAttrList.GetTrailingTrivia().Insert(0, todoComment));
                }

                result.Add(newAttrList);
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

    private (AttributeSyntax attribute, List<string> unsupportedProperties) TransformTestCaseAttribute(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList == null)
        {
            return (attribute, new List<string>());
        }

        var newArgs = new List<AttributeArgumentSyntax>();
        ExpressionSyntax? expectedValue = null;
        var unsupportedProperties = new List<string>();

        foreach (var arg in attribute.ArgumentList.Arguments)
        {
            var namedProperty = arg.NameEquals?.Name.Identifier.Text;

            if (namedProperty == "ExpectedResult")
            {
                expectedValue = arg.Expression;
            }
            else if (namedProperty == null)
            {
                // Positional argument - keep it
                newArgs.Add(arg);
            }
            else if (namedProperty == "Ignore" || namedProperty == "IgnoreReason")
            {
                // Map NUnit's Ignore/IgnoreReason to TUnit's Skip
                var skipArg = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Skip")),
                    null,
                    arg.Expression);
                newArgs.Add(skipArg);
            }
            else if (namedProperty is "TestName" or "Category" or "Description" or "Author" or "Explicit" or "ExplicitReason")
            {
                // These properties don't have direct TUnit equivalents
                unsupportedProperties.Add($"{namedProperty} = {arg.Expression}");
            }
            // Other named arguments are preserved as-is (they might be TUnit-compatible)
            else
            {
                newArgs.Add(arg);
            }
        }

        // Add expected value as last positional argument
        if (expectedValue != null)
        {
            newArgs.Add(SyntaxFactory.AttributeArgument(expectedValue));
        }

        var newAttribute = attribute.WithArgumentList(
            SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(newArgs)));

        // The attribute will be renamed to "Arguments" by the existing attribute rewriter
        return (newAttribute, unsupportedProperties);
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
