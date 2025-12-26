# NUnit ExpectedResult Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add code fixer support for migrating NUnit's `ExpectedResult` pattern to TUnit's assertion-based approach.

**Architecture:** Extend the existing `NUnitMigrationCodeFixProvider` with a new `NUnitExpectedResultRewriter` that transforms `[TestCase(..., ExpectedResult = X)]` into `[Arguments(..., X)]` with an assertion in the method body. The rewriter runs in the `ApplyFrameworkSpecificConversions` hook before attribute conversion.

**Tech Stack:** Roslyn CodeAnalysis, C# Syntax Rewriters, existing TUnit.Analyzers infrastructure.

---

## Task 1: Add Failing Test for Simple ExpectedResult

**Files:**
- Modify: `TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs`

**Step 1: Write the failing test**

Add this test method at the end of the test class (before the `ConfigureNUnitTest` methods):

```csharp
[Test]
public async Task NUnit_ExpectedResult_Converted()
{
    await CodeFixer.VerifyCodeFixAsync(
        """
            using NUnit.Framework;

            {|#0:public class MyClass|}
            {
                [TestCase(2, 3, ExpectedResult = 5)]
                public int Add(int a, int b) => a + b;
            }
            """,
        Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
        """
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments(2, 3, 5)]
                public async Task Add(int a, int b, int expected)
                {
                    await Assert.That(a + b).IsEqualTo(expected);
                }
            }
            """,
        ConfigureNUnitTest
    );
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnit_ExpectedResult_Converted"`

Expected: FAIL (code fixer doesn't handle ExpectedResult yet)

**Step 3: Commit**

```bash
git add TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs
git commit -m "test: add failing test for NUnit ExpectedResult migration"
```

---

## Task 2: Create NUnitExpectedResultRewriter Class

**Files:**
- Create: `TUnit.Analyzers.CodeFixers/NUnitExpectedResultRewriter.cs`

**Step 1: Create the rewriter skeleton**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

/// <summary>
/// Transforms NUnit [TestCase(..., ExpectedResult = X)] patterns to TUnit assertions.
/// </summary>
public class NUnitExpectedResultRewriter : CSharpSyntaxRewriter
{
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

        return attribute.ArgumentList.Arguments
            .Any(arg => arg.NameEquals?.Name.Identifier.Text == "ExpectedResult");
    }

    private MethodDeclarationSyntax TransformMethod(
        MethodDeclarationSyntax method,
        List<AttributeSyntax> testCaseAttributes,
        TypeSyntax originalReturnType)
    {
        // 1. Add 'expected' parameter
        var expectedParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier("expected"))
            .WithType(originalReturnType.WithTrailingTrivia(SyntaxFactory.Space));

        var newParameters = method.ParameterList.AddParameters(expectedParam);

        // 2. Change return type to async Task
        var asyncTaskType = SyntaxFactory.ParseTypeName("async Task ")
            .WithTrailingTrivia(SyntaxFactory.Space);

        // 3. Transform the body
        var newBody = TransformBody(method, originalReturnType);

        // 4. Build the new method
        var newMethod = method
            .WithReturnType(SyntaxFactory.ParseTypeName("Task").WithTrailingTrivia(SyntaxFactory.Space))
            .WithParameterList(newParameters)
            .WithBody(newBody)
            .WithExpressionBody(null)
            .WithSemicolonToken(default);

        // 5. Add async modifier if not present
        if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);
            newMethod = newMethod.AddModifiers(asyncModifier);
        }

        // 6. Update attribute lists (remove ExpectedResult from TestCase, add [Test])
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
            var returnStatements = method.Body.Statements
                .OfType<ReturnStatementSyntax>()
                .ToList();

            if (returnStatements.Count == 1 && returnStatements[0].Expression != null)
            {
                // Single return - use the expression directly
                returnExpression = returnStatements[0].Expression;

                // Build new body with all statements except the return, plus assertion
                var statementsWithoutReturn = method.Body.Statements
                    .Where(s => s != returnStatements[0])
                    .ToList();

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
            SyntaxFactory.VariableDeclaration(returnType.WithTrailingTrivia(SyntaxFactory.Space))
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
                    // Transform TestCase with ExpectedResult to Arguments
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
        if (!hasTestAttribute)
        {
            var testAttr = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test"));
            var testAttrList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(testAttr))
                .WithLeadingTrivia(attributeLists.First().GetLeadingTrivia());
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
```

**Step 2: Build to verify no syntax errors**

Run: `dotnet build TUnit.Analyzers.CodeFixers`

Expected: Build succeeded

**Step 3: Commit**

```bash
git add TUnit.Analyzers.CodeFixers/NUnitExpectedResultRewriter.cs
git commit -m "feat: add NUnitExpectedResultRewriter skeleton"
```

---

## Task 3: Integrate Rewriter into Code Fix Provider

**Files:**
- Modify: `TUnit.Analyzers.CodeFixers/NUnitMigrationCodeFixProvider.cs`

**Step 1: Update ApplyFrameworkSpecificConversions**

Replace the `ApplyFrameworkSpecificConversions` method:

```csharp
protected override CompilationUnitSyntax ApplyFrameworkSpecificConversions(CompilationUnitSyntax compilationUnit, SemanticModel semanticModel, Compilation compilation)
{
    // Transform ExpectedResult patterns before attribute conversion
    var expectedResultRewriter = new NUnitExpectedResultRewriter(semanticModel);
    compilationUnit = (CompilationUnitSyntax)expectedResultRewriter.Visit(compilationUnit);

    return compilationUnit;
}
```

**Step 2: Run the test**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnit_ExpectedResult_Converted"`

Expected: May pass or fail depending on edge cases - we'll iterate

**Step 3: Commit**

```bash
git add TUnit.Analyzers.CodeFixers/NUnitMigrationCodeFixProvider.cs
git commit -m "feat: integrate ExpectedResult rewriter into NUnit migration"
```

---

## Task 4: Fix Formatting Issues

**Files:**
- Modify: `TUnit.Analyzers.CodeFixers/NUnitExpectedResultRewriter.cs`

**Step 1: Run test and check output**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnit_ExpectedResult_Converted" -v n`

Examine the actual output vs expected - likely issues with:
- Trivia (whitespace, newlines)
- Attribute ordering
- Method modifier ordering

**Step 2: Fix identified issues**

Common fixes needed:
- Add proper leading/trailing trivia to statements
- Ensure async modifier is in correct position
- Fix attribute list formatting

**Step 3: Run test again**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnit_ExpectedResult_Converted"`

Expected: PASS

**Step 4: Commit**

```bash
git add TUnit.Analyzers.CodeFixers/NUnitExpectedResultRewriter.cs
git commit -m "fix: correct formatting in ExpectedResult transformation"
```

---

## Task 5: Add Test for Multiple TestCase Attributes

**Files:**
- Modify: `TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs`

**Step 1: Write the test**

```csharp
[Test]
public async Task NUnit_Multiple_ExpectedResult_Converted()
{
    await CodeFixer.VerifyCodeFixAsync(
        """
            using NUnit.Framework;

            {|#0:public class MyClass|}
            {
                [TestCase(2, 3, ExpectedResult = 5)]
                [TestCase(10, 5, ExpectedResult = 15)]
                [TestCase(0, 0, ExpectedResult = 0)]
                public int Add(int a, int b) => a + b;
            }
            """,
        Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
        """
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments(2, 3, 5)]
                [Arguments(10, 5, 15)]
                [Arguments(0, 0, 0)]
                public async Task Add(int a, int b, int expected)
                {
                    await Assert.That(a + b).IsEqualTo(expected);
                }
            }
            """,
        ConfigureNUnitTest
    );
}
```

**Step 2: Run test**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnit_Multiple_ExpectedResult_Converted"`

Expected: PASS

**Step 3: Commit**

```bash
git add TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs
git commit -m "test: add test for multiple TestCase with ExpectedResult"
```

---

## Task 6: Add Test for Block-Bodied Method with Single Return

**Files:**
- Modify: `TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs`

**Step 1: Write the test**

```csharp
[Test]
public async Task NUnit_ExpectedResult_BlockBody_SingleReturn_Converted()
{
    await CodeFixer.VerifyCodeFixAsync(
        """
            using NUnit.Framework;

            {|#0:public class MyClass|}
            {
                [TestCase(2, 3, ExpectedResult = 5)]
                public int Add(int a, int b)
                {
                    var sum = a + b;
                    return sum;
                }
            }
            """,
        Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
        """
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments(2, 3, 5)]
                public async Task Add(int a, int b, int expected)
                {
                    var sum = a + b;
                    await Assert.That(sum).IsEqualTo(expected);
                }
            }
            """,
        ConfigureNUnitTest
    );
}
```

**Step 2: Run test**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnit_ExpectedResult_BlockBody_SingleReturn_Converted"`

Expected: PASS (or fix if needed)

**Step 3: Commit**

```bash
git add TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs
git commit -m "test: add test for block-bodied ExpectedResult with single return"
```

---

## Task 7: Add Test for Multiple Return Statements

**Files:**
- Modify: `TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs`

**Step 1: Write the test**

```csharp
[Test]
public async Task NUnit_ExpectedResult_MultipleReturns_Converted()
{
    await CodeFixer.VerifyCodeFixAsync(
        """
            using NUnit.Framework;

            {|#0:public class MyClass|}
            {
                [TestCase(-1, ExpectedResult = 0)]
                [TestCase(0, ExpectedResult = 1)]
                [TestCase(5, ExpectedResult = 120)]
                public int Factorial(int n)
                {
                    if (n < 0) return 0;
                    if (n <= 1) return 1;
                    return n * Factorial(n - 1);
                }
            }
            """,
        Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
        """
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments(-1, 0)]
                [Arguments(0, 1)]
                [Arguments(5, 120)]
                public async Task Factorial(int n, int expected)
                {
                    int result;
                    if (n < 0) result = 0;
                    else if (n <= 1) result = 1;
                    else result = n * Factorial(n - 1);
                    await Assert.That(result).IsEqualTo(expected);
                }
            }
            """,
        ConfigureNUnitTest
    );
}
```

**Step 2: Run test**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnit_ExpectedResult_MultipleReturns_Converted"`

Expected: May fail initially - multiple returns require more complex transformation

**Step 3: Fix if needed and commit**

```bash
git add TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs
git commit -m "test: add test for ExpectedResult with multiple returns"
```

---

## Task 8: Add Test for String ExpectedResult

**Files:**
- Modify: `TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs`

**Step 1: Write the test**

```csharp
[Test]
public async Task NUnit_ExpectedResult_String_Converted()
{
    await CodeFixer.VerifyCodeFixAsync(
        """
            using NUnit.Framework;

            {|#0:public class MyClass|}
            {
                [TestCase("hello", ExpectedResult = "HELLO")]
                [TestCase("World", ExpectedResult = "WORLD")]
                public string ToUpper(string input) => input.ToUpper();
            }
            """,
        Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
        """
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments("hello", "HELLO")]
                [Arguments("World", "WORLD")]
                public async Task ToUpper(string input, string expected)
                {
                    await Assert.That(input.ToUpper()).IsEqualTo(expected);
                }
            }
            """,
        ConfigureNUnitTest
    );
}
```

**Step 2: Run test**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnit_ExpectedResult_String_Converted"`

Expected: PASS

**Step 3: Commit**

```bash
git add TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs
git commit -m "test: add test for string ExpectedResult migration"
```

---

## Task 9: Run Full Test Suite

**Files:** None (verification only)

**Step 1: Run all NUnit migration tests**

Run: `dotnet test TUnit.Analyzers.Tests --filter "NUnitMigration"`

Expected: All tests PASS

**Step 2: Run full analyzer test suite**

Run: `dotnet test TUnit.Analyzers.Tests`

Expected: All tests PASS

**Step 3: Commit any remaining fixes**

```bash
git add -A
git commit -m "fix: address any remaining test failures"
```

---

## Task 10: Update Design Document Status

**Files:**
- Modify: `docs/plans/2025-12-25-nunit-expectedresult-migration-design.md`

**Step 1: Update status**

Change the Status line from:
```
**Status**: Design Complete
```

To:
```
**Status**: Implemented
```

**Step 2: Commit**

```bash
git add docs/plans/2025-12-25-nunit-expectedresult-migration-design.md
git commit -m "docs: mark ExpectedResult migration as implemented"
```

---

## Verification Checklist

After all tasks complete:

- [ ] `dotnet test TUnit.Analyzers.Tests` passes
- [ ] `dotnet build TUnit.Analyzers.CodeFixers` succeeds
- [ ] New tests cover: simple ExpectedResult, multiple TestCase, block body, multiple returns, string types
- [ ] Code follows existing patterns in NUnitMigrationCodeFixProvider
