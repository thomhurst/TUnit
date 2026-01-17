using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base;
using TUnit.Analyzers.CodeFixers.Base.TwoPhase;
using TUnit.Analyzers.CodeFixers.TwoPhase;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MSTestMigrationCodeFixProvider)), Shared]
public class MSTestMigrationCodeFixProvider : BaseMigrationCodeFixProvider
{
    protected override string FrameworkName => "MSTest";
    protected override string DiagnosticId => Rules.MSTestMigration.Id;
    protected override string CodeFixTitle => "Convert MSTest code to TUnit";

    protected override bool ShouldAddTUnitUsings() => true;

    protected override MigrationAnalyzer? CreateTwoPhaseAnalyzer(SemanticModel semanticModel, Compilation compilation)
    {
        return new MSTestTwoPhaseAnalyzer(semanticModel, compilation);
    }

    // The following methods are required by the base class but are only used in the legacy
    // conversion path. The two-phase analyzer handles these conversions directly.

    protected override AttributeRewriter CreateAttributeRewriter(Compilation compilation)
    {
        return new MSTestAttributeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateAssertionRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new MSTestAssertionRewriter(semanticModel);
    }

    protected override CSharpSyntaxRewriter CreateBaseTypeRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new MSTestBaseTypeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateLifecycleRewriter(Compilation compilation)
    {
        return new MSTestLifecycleRewriter();
    }

    protected override CompilationUnitSyntax ApplyFrameworkSpecificConversions(CompilationUnitSyntax compilationUnit, SemanticModel semanticModel, Compilation compilation)
    {
        // Handle [ExpectedException] attribute conversion
        var expectedExceptionRewriter = new MSTestExpectedExceptionRewriter();
        compilationUnit = (CompilationUnitSyntax)expectedExceptionRewriter.Visit(compilationUnit);

        return compilationUnit;
    }
}

public class MSTestAttributeRewriter : AttributeRewriter
{
    protected override string FrameworkName => "MSTest";
    
    protected override bool IsFrameworkAttribute(string attributeName)
    {
        return attributeName switch
        {
            "TestClass" or "TestMethod" or "DataRow" or "DynamicData" or
            "TestInitialize" or "TestCleanup" or "ClassInitialize" or "ClassCleanup" or
            "TestCategory" or "Ignore" or "Priority" or "Owner" => true,
            _ => false
        };
    }
    
    protected override AttributeArgumentListSyntax? ConvertAttributeArguments(AttributeArgumentListSyntax argumentList, string attributeName)
    {
        return attributeName switch
        {
            "DataRow" => argumentList, // Arguments attribute uses the same format
            "DynamicData" => ConvertDynamicDataArguments(argumentList),
            "TestCategory" => ConvertTestCategoryArguments(argumentList),
            "Priority" => ConvertPriorityArguments(argumentList),
            "Owner" => ConvertOwnerArguments(argumentList),
            "ClassInitialize" or "ClassCleanup" => null, // These don't need arguments in TUnit
            _ => argumentList
        };
    }
    
    private AttributeArgumentListSyntax ConvertDynamicDataArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert DynamicData to MethodDataSource
        if (argumentList.Arguments.Count > 0)
        {
            var firstArg = argumentList.Arguments[0];
            
            // If it's a nameof expression, keep it as is
            if (firstArg.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } })
            {
                return SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(firstArg)
                );
            }
            
            // If it's a string literal, keep just the method name
            if (firstArg.Expression is LiteralExpressionSyntax literal)
            {
                return SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(firstArg)
                );
            }
        }
        
        return argumentList;
    }
    
    private AttributeArgumentListSyntax ConvertTestCategoryArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert TestCategory to Property
        var arguments = new List<AttributeArgumentSyntax>();
        
        arguments.Add(SyntaxFactory.AttributeArgument(
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, 
                SyntaxFactory.Literal("Category"))
        ));
        
        if (argumentList.Arguments.Count > 0)
        {
            arguments.Add(argumentList.Arguments[0]);
        }
        
        return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments));
    }
    
    private AttributeArgumentListSyntax ConvertPriorityArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert Priority to Property
        var arguments = new List<AttributeArgumentSyntax>();

        arguments.Add(SyntaxFactory.AttributeArgument(
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal("Priority"))
        ));

        if (argumentList.Arguments.Count > 0)
        {
            arguments.Add(SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(argumentList.Arguments[0].Expression.ToString()))
            ));
        }

        return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments));
    }

    private AttributeArgumentListSyntax ConvertOwnerArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert Owner to Property
        var arguments = new List<AttributeArgumentSyntax>();

        arguments.Add(SyntaxFactory.AttributeArgument(
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal("Owner"))
        ));

        if (argumentList.Arguments.Count > 0)
        {
            arguments.Add(argumentList.Arguments[0]);
        }

        return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments));
    }

    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        // Handle ClassInitialize and ClassCleanup specially - they need static context parameter removed
        // Process all attributes, don't return early to preserve sibling attributes
        var attributes = new List<AttributeSyntax>();
        bool hasClassLifecycleAttribute = false;

        foreach (var attribute in node.Attributes)
        {
            var attributeName = MigrationHelpers.GetAttributeName(attribute);

            if (attributeName is "ClassInitialize" or "ClassCleanup")
            {
                hasClassLifecycleAttribute = true;
                var hookType = attributeName == "ClassInitialize" ? "Before" : "After";
                var newAttribute = SyntaxFactory.Attribute(
                    SyntaxFactory.IdentifierName(hookType),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("HookType"),
                                    SyntaxFactory.IdentifierName("Class")
                                )
                            )
                        )
                    )
                );
                attributes.Add(newAttribute);
                // Don't return early - continue processing other attributes!
            }
            else
            {
                // For non-ClassInitialize/ClassCleanup, use base conversion logic
                var converted = ConvertAttribute(attribute);
                if (converted != null)
                {
                    attributes.Add(converted);
                }
            }
        }

        // If we processed class lifecycle attributes, return our combined list
        if (hasClassLifecycleAttribute)
        {
            return attributes.Count > 0
                ? node.WithAttributes(SyntaxFactory.SeparatedList(attributes))
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia())
                : null;
        }

        return base.VisitAttributeList(node);
    }
}

public class MSTestAssertionRewriter : AssertionRewriter
{
    protected override string FrameworkName => "MSTest";
    
    public MSTestAssertionRewriter(SemanticModel semanticModel) : base(semanticModel)
    {
    }
    
    protected override bool IsFrameworkAssertionNamespace(string namespaceName)
    {
        return namespaceName == "Microsoft.VisualStudio.TestTools.UnitTesting" ||
               namespaceName.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting.");
    }

    protected override bool IsKnownAssertionTypeBySyntax(string targetType, string methodName)
    {
        // MSTest assertion types that can be detected by syntax
        return targetType is "Assert" or "CollectionAssert" or "StringAssert" or "FileAssert" or "DirectoryAssert";
    }

    protected override ExpressionSyntax? ConvertAssertionIfNeeded(InvocationExpressionSyntax invocation)
    {
        // First try semantic analysis
        var isFrameworkAssertionViaSemantic = false;
        try
        {
            isFrameworkAssertionViaSemantic = IsFrameworkAssertion(invocation);
        }
        catch (InvalidOperationException)
        {
            // Semantic analysis failed due to invalid compilation state, fall back to syntax-based detection
        }
        catch (ArgumentException)
        {
            // Semantic analysis failed due to invalid arguments, fall back to syntax-based detection
        }

        // Check if it looks like an MSTest assertion syntactically
        var isMsTestAssertionSyntax = invocation.Expression is MemberAccessExpressionSyntax ma &&
                                       ma.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" or "CollectionAssert" or "StringAssert" or "DirectoryAssert" or "FileAssert" };

        if (!isFrameworkAssertionViaSemantic && !isMsTestAssertionSyntax)
        {
            return null;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" })
        {
            return ConvertMSTestAssertion(invocation, memberAccess.Name.Identifier.Text);
        }

        // Handle CollectionAssert
        if (invocation.Expression is MemberAccessExpressionSyntax collectionAccess &&
            collectionAccess.Expression is IdentifierNameSyntax { Identifier.Text: "CollectionAssert" })
        {
            return ConvertCollectionAssertion(invocation, collectionAccess.Name.Identifier.Text);
        }

        // Handle StringAssert
        if (invocation.Expression is MemberAccessExpressionSyntax stringAccess &&
            stringAccess.Expression is IdentifierNameSyntax { Identifier.Text: "StringAssert" })
        {
            return ConvertStringAssertion(invocation, stringAccess.Name.Identifier.Text);
        }

        // Handle DirectoryAssert
        if (invocation.Expression is MemberAccessExpressionSyntax directoryAccess &&
            directoryAccess.Expression is IdentifierNameSyntax { Identifier.Text: "DirectoryAssert" })
        {
            return ConvertDirectoryAssertion(invocation, directoryAccess.Name.Identifier.Text);
        }

        // Handle FileAssert
        if (invocation.Expression is MemberAccessExpressionSyntax fileAccess &&
            fileAccess.Expression is IdentifierNameSyntax { Identifier.Text: "FileAssert" })
        {
            return ConvertFileAssertion(invocation, fileAccess.Name.Identifier.Text);
        }

        return null;
    }
    
    private ExpressionSyntax? ConvertMSTestAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // MSTest assertion message parameter positions:
        // - 2-arg assertions (IsTrue, IsFalse, IsNull, IsNotNull): message is 2nd param (index 1)
        // - 3-arg assertions (AreEqual, AreSame, etc.): message is 3rd param (index 2)

        return methodName switch
        {
            // Equality assertions - check for comparer overloads and format strings
            "AreEqual" => ConvertAreEqual(arguments),
            "AreNotEqual" => ConvertAreNotEqual(arguments),
            "AreSame" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsSameReferenceAs", arguments[1].Expression, arguments[2].Expression, arguments[0]),
            "AreSame" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsSameReferenceAs", arguments[1].Expression, arguments[0]),
            "AreNotSame" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsNotSameReferenceAs", arguments[1].Expression, arguments[2].Expression, arguments[0]),
            "AreNotSame" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsNotSameReferenceAs", arguments[1].Expression, arguments[0]),

            // 1-arg assertions with message as 2nd param
            "IsTrue" when arguments.Count >= 2 =>
                CreateTUnitAssertionWithMessage("IsTrue", arguments[0].Expression, arguments[1].Expression),
            "IsTrue" when arguments.Count >= 1 =>
                CreateTUnitAssertion("IsTrue", arguments[0].Expression),
            "IsFalse" when arguments.Count >= 2 =>
                CreateTUnitAssertionWithMessage("IsFalse", arguments[0].Expression, arguments[1].Expression),
            "IsFalse" when arguments.Count >= 1 =>
                CreateTUnitAssertion("IsFalse", arguments[0].Expression),
            "IsNull" when arguments.Count >= 2 =>
                CreateTUnitAssertionWithMessage("IsNull", arguments[0].Expression, arguments[1].Expression),
            "IsNull" when arguments.Count >= 1 =>
                CreateTUnitAssertion("IsNull", arguments[0].Expression),
            "IsNotNull" when arguments.Count >= 2 =>
                CreateTUnitAssertionWithMessage("IsNotNull", arguments[0].Expression, arguments[1].Expression),
            "IsNotNull" when arguments.Count >= 1 =>
                CreateTUnitAssertion("IsNotNull", arguments[0].Expression),

            // Type assertions with message as 3rd param
            "IsInstanceOfType" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsAssignableTo", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "IsInstanceOfType" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsAssignableTo", arguments[0].Expression, arguments[1]),
            "IsNotInstanceOfType" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsNotAssignableTo", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "IsNotInstanceOfType" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsNotAssignableTo", arguments[0].Expression, arguments[1]),

            // Special assertions
            "ThrowsException" when arguments.Count >= 1 =>
                CreateThrowsAssertion(invocation),
            "ThrowsExceptionAsync" when arguments.Count >= 1 =>
                CreateThrowsAsyncAssertion(invocation),
            "Fail" => CreateFailAssertion(arguments),
            "Inconclusive" => CreateInconclusiveAssertion(arguments),
            _ => null
        };
    }

    /// <summary>
    /// Converts Assert.AreEqual with support for comparer overloads and format string messages.
    /// MSTest overloads:
    /// - Assert.AreEqual(expected, actual)
    /// - Assert.AreEqual(expected, actual, message)
    /// - Assert.AreEqual(expected, actual, message, params object[] parameters)
    /// - Assert.AreEqual(expected, actual, comparer)
    /// - Assert.AreEqual(expected, actual, comparer, message)
    /// </summary>
    private ExpressionSyntax? ConvertAreEqual(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        if (arguments.Count < 2)
        {
            return null;
        }

        var expected = arguments[0];
        var actual = arguments[1];

        // 2 args: AreEqual(expected, actual)
        if (arguments.Count == 2)
        {
            return CreateTUnitAssertion("IsEqualTo", actual.Expression, expected);
        }

        // 3+ args: Determine if 3rd arg is a message (string) or comparer
        // Check for named arguments first (most reliable)
        var thirdArg = arguments[2];
        var namedArg = thirdArg.NameColon?.Name.Identifier.Text;
        if (namedArg == "message")
        {
            var (msg, fmtArgs) = ExtractMessageWithFormatArgs(arguments, 2);
            if (msg != null)
            {
                var msgExpr = CreateMessageExpression(msg, fmtArgs);
                return CreateTUnitAssertionWithMessage("IsEqualTo", actual.Expression, msgExpr, expected);
            }
            return CreateTUnitAssertion("IsEqualTo", actual.Expression, expected);
        }
        if (namedArg == "comparer")
        {
            var result = CreateTUnitAssertion("IsEqualTo", actual.Expression, expected);
            if (arguments.Count >= 4)
            {
                var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 3);
                if (message != null)
                {
                    var messageExpr = CreateMessageExpression(message, formatArgs);
                    result = CreateTUnitAssertionWithMessage("IsEqualTo", actual.Expression, messageExpr, expected);
                }
            }
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - IEqualityComparer was used. TUnit uses .IsEqualTo() which may have different comparison semantics."),
                SyntaxFactory.EndOfLine("\n"));
        }

        // Check syntactically if it looks like a string (message)
        var isLikelyMessage = thirdArg.Expression is LiteralExpressionSyntax literal &&
                               literal.IsKind(SyntaxKind.StringLiteralExpression);
        // Also check for interpolated strings
        isLikelyMessage = isLikelyMessage || thirdArg.Expression is InterpolatedStringExpressionSyntax;

        // If it's a string expression, treat as message
        if (isLikelyMessage)
        {
            var (msg, fmtArgs) = ExtractMessageWithFormatArgs(arguments, 2);
            if (msg != null)
            {
                var msgExpr = CreateMessageExpression(msg, fmtArgs);
                return CreateTUnitAssertionWithMessage("IsEqualTo", actual.Expression, msgExpr, expected);
            }
            return CreateTUnitAssertion("IsEqualTo", actual.Expression, expected);
        }

        // If not a string literal, try semantic analysis to check for comparer
        var isComparer = IsLikelyComparerArgumentSafe(arguments[2]);

        if (isComparer == true)
        {
            // AreEqual(expected, actual, comparer) or AreEqual(expected, actual, comparer, message)
            var result = CreateTUnitAssertion("IsEqualTo", actual.Expression, expected);
            if (arguments.Count >= 4)
            {
                // Has message after comparer
                var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 3);
                if (message != null)
                {
                    var messageExpr = CreateMessageExpression(message, formatArgs);
                    result = CreateTUnitAssertionWithMessage("IsEqualTo", actual.Expression, messageExpr, expected);
                }
            }
            // Add TODO for comparer
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - IEqualityComparer was used. TUnit uses .IsEqualTo() which may have different comparison semantics."),
                SyntaxFactory.EndOfLine("\n"));
        }

        if (isComparer == null)
        {
            // Type couldn't be determined - add TODO for manual review
            return CreateTUnitAssertion("IsEqualTo", actual.Expression, expected).WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - third argument could not be identified as comparer or message. Manual verification required."),
                SyntaxFactory.EndOfLine("\n"));
        }

        // isComparer == false: Not a comparer, treat remaining args as message with format args
        var (msg2, fmtArgs2) = ExtractMessageWithFormatArgs(arguments, 2);
        if (msg2 != null)
        {
            var msgExpr = CreateMessageExpression(msg2, fmtArgs2);
            return CreateTUnitAssertionWithMessage("IsEqualTo", actual.Expression, msgExpr, expected);
        }

        return CreateTUnitAssertion("IsEqualTo", actual.Expression, expected);
    }

    /// <summary>
    /// Safely checks if an argument is a comparer, catching any exceptions from semantic analysis.
    /// Returns null if the type cannot be determined.
    /// </summary>
    private bool? IsLikelyComparerArgumentSafe(ArgumentSyntax argument)
    {
        try
        {
            return IsLikelyComparerArgument(argument);
        }
        catch (InvalidOperationException)
        {
            // Semantic analysis failed due to invalid compilation state
            return null;
        }
        catch (ArgumentException)
        {
            // Semantic analysis failed due to invalid arguments
            return null;
        }
    }

    /// <summary>
    /// Converts Assert.AreNotEqual with support for comparer overloads and format string messages.
    /// </summary>
    private ExpressionSyntax? ConvertAreNotEqual(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        if (arguments.Count < 2)
        {
            return null;
        }

        var expected = arguments[0];
        var actual = arguments[1];

        // 2 args: AreNotEqual(expected, actual)
        if (arguments.Count == 2)
        {
            return CreateTUnitAssertion("IsNotEqualTo", actual.Expression, expected);
        }

        // 3+ args: Determine if 3rd arg is a message (string) or comparer
        // Check for named arguments first (most reliable)
        var thirdArg = arguments[2];
        var namedArg = thirdArg.NameColon?.Name.Identifier.Text;
        if (namedArg == "message")
        {
            var (msg, fmtArgs) = ExtractMessageWithFormatArgs(arguments, 2);
            if (msg != null)
            {
                var msgExpr = CreateMessageExpression(msg, fmtArgs);
                return CreateTUnitAssertionWithMessage("IsNotEqualTo", actual.Expression, msgExpr, expected);
            }
            return CreateTUnitAssertion("IsNotEqualTo", actual.Expression, expected);
        }
        if (namedArg == "comparer")
        {
            var result = CreateTUnitAssertion("IsNotEqualTo", actual.Expression, expected);
            if (arguments.Count >= 4)
            {
                var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 3);
                if (message != null)
                {
                    var messageExpr = CreateMessageExpression(message, formatArgs);
                    result = CreateTUnitAssertionWithMessage("IsNotEqualTo", actual.Expression, messageExpr, expected);
                }
            }
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - IEqualityComparer was used. TUnit uses .IsNotEqualTo() which may have different comparison semantics."),
                SyntaxFactory.EndOfLine("\n"));
        }

        // Check syntactically if it looks like a string (message)
        var isLikelyMessage = thirdArg.Expression is LiteralExpressionSyntax literal &&
                               literal.IsKind(SyntaxKind.StringLiteralExpression);
        // Also check for interpolated strings
        isLikelyMessage = isLikelyMessage || thirdArg.Expression is InterpolatedStringExpressionSyntax;

        // If it's a string expression, treat as message
        if (isLikelyMessage)
        {
            var (msg, fmtArgs) = ExtractMessageWithFormatArgs(arguments, 2);
            if (msg != null)
            {
                var msgExpr = CreateMessageExpression(msg, fmtArgs);
                return CreateTUnitAssertionWithMessage("IsNotEqualTo", actual.Expression, msgExpr, expected);
            }
            return CreateTUnitAssertion("IsNotEqualTo", actual.Expression, expected);
        }

        // If not a string literal, try semantic analysis to check for comparer
        var isComparer = IsLikelyComparerArgumentSafe(arguments[2]);

        if (isComparer == true)
        {
            // AreNotEqual(expected, actual, comparer) or AreNotEqual(expected, actual, comparer, message)
            var result = CreateTUnitAssertion("IsNotEqualTo", actual.Expression, expected);
            if (arguments.Count >= 4)
            {
                // Has message after comparer
                var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 3);
                if (message != null)
                {
                    var messageExpr = CreateMessageExpression(message, formatArgs);
                    result = CreateTUnitAssertionWithMessage("IsNotEqualTo", actual.Expression, messageExpr, expected);
                }
            }
            // Add TODO for comparer
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - IEqualityComparer was used. TUnit uses .IsNotEqualTo() which may have different comparison semantics."),
                SyntaxFactory.EndOfLine("\n"));
        }

        if (isComparer == null)
        {
            // Type couldn't be determined - add TODO for manual review
            return CreateTUnitAssertion("IsNotEqualTo", actual.Expression, expected).WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - third argument could not be identified as comparer or message. Manual verification required."),
                SyntaxFactory.EndOfLine("\n"));
        }

        // isComparer == false: Not a comparer, treat remaining args as message with format args
        var (msg2, fmtArgs2) = ExtractMessageWithFormatArgs(arguments, 2);
        if (msg2 != null)
        {
            var msgExpr = CreateMessageExpression(msg2, fmtArgs2);
            return CreateTUnitAssertionWithMessage("IsNotEqualTo", actual.Expression, msgExpr, expected);
        }

        return CreateTUnitAssertion("IsNotEqualTo", actual.Expression, expected);
    }

    private ExpressionSyntax CreateInconclusiveAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // Convert Assert.Inconclusive(message) to await Assert.Skip(message)
        var skipInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("Skip")
            ),
            arguments.Count > 0
                ? SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(arguments[0]))
                : SyntaxFactory.ArgumentList()
        );

        return SyntaxFactory.AwaitExpression(skipInvocation);
    }
    
    private ExpressionSyntax? ConvertCollectionAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // CollectionAssert message is typically the last parameter after the required args

        return methodName switch
        {
            // Equality assertions
            "AreEqual" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsEquivalentTo", arguments[1].Expression, arguments[2].Expression, arguments[0]),
            "AreEqual" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsEquivalentTo", arguments[1].Expression, arguments[0]),
            "AreNotEqual" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsNotEquivalentTo", arguments[1].Expression, arguments[2].Expression, arguments[0]),
            "AreNotEqual" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsNotEquivalentTo", arguments[1].Expression, arguments[0]),

            // AreEquivalent (order independent)
            "AreEquivalent" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsEquivalentTo", arguments[1].Expression, arguments[2].Expression, arguments[0]),
            "AreEquivalent" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsEquivalentTo", arguments[1].Expression, arguments[0]),
            "AreNotEquivalent" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsNotEquivalentTo", arguments[1].Expression, arguments[2].Expression, arguments[0]),
            "AreNotEquivalent" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsNotEquivalentTo", arguments[1].Expression, arguments[0]),

            // Contains assertions
            "Contains" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("Contains", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "Contains" when arguments.Count >= 2 =>
                CreateTUnitAssertion("Contains", arguments[0].Expression, arguments[1]),
            "DoesNotContain" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("DoesNotContain", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "DoesNotContain" when arguments.Count >= 2 =>
                CreateTUnitAssertion("DoesNotContain", arguments[0].Expression, arguments[1]),

            // Subset/Superset
            "IsSubsetOf" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsSubsetOf", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "IsSubsetOf" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsSubsetOf", arguments[0].Expression, arguments[1]),
            "IsNotSubsetOf" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("IsNotSubsetOf", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "IsNotSubsetOf" when arguments.Count >= 2 =>
                CreateTUnitAssertion("IsNotSubsetOf", arguments[0].Expression, arguments[1]),

            // Unique items
            "AllItemsAreUnique" when arguments.Count >= 2 =>
                CreateTUnitAssertionWithMessage("HasDistinctItems", arguments[0].Expression, arguments[1].Expression),
            "AllItemsAreUnique" when arguments.Count >= 1 =>
                CreateTUnitAssertion("HasDistinctItems", arguments[0].Expression),

            // AllItemsAreNotNull
            "AllItemsAreNotNull" when arguments.Count >= 2 =>
                CreateAllItemsAreNotNullWithMessage(arguments[0].Expression, arguments[1].Expression),
            "AllItemsAreNotNull" when arguments.Count >= 1 =>
                CreateTUnitAssertion("All", arguments[0].Expression,
                    SyntaxFactory.Argument(CreateNotNullLambda())),

            // AllItemsAreInstancesOfType
            "AllItemsAreInstancesOfType" when arguments.Count >= 3 =>
                CreateAllItemsAreInstancesOfTypeWithMessage(arguments[0].Expression, arguments[1].Expression, arguments[2].Expression),
            "AllItemsAreInstancesOfType" when arguments.Count >= 2 =>
                CreateAllItemsAreInstancesOfType(arguments[0].Expression, arguments[1].Expression),

            _ => null
        };
    }

    private ExpressionSyntax CreateAllItemsAreNotNullWithMessage(ExpressionSyntax collection, ExpressionSyntax message)
    {
        return CreateTUnitAssertionWithMessage("All", collection, message,
            SyntaxFactory.Argument(CreateNotNullLambda()));
    }

    private static ExpressionSyntax CreateNotNullLambda()
    {
        return SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                SyntaxFactory.IdentifierName("x"),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            )
        ).WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
            .WithTrailingTrivia(SyntaxFactory.Space));
    }

    private ExpressionSyntax CreateAllItemsAreInstancesOfType(ExpressionSyntax collection, ExpressionSyntax expectedType)
    {
        // Use All with type check: x => expectedType.IsInstanceOfType(x)
        var result = CreateTUnitAssertion("All", collection,
            SyntaxFactory.Argument(CreateIsInstanceOfTypeLambda(expectedType)));
        return result;
    }

    private static ExpressionSyntax CreateIsInstanceOfTypeLambda(ExpressionSyntax expectedType)
    {
        return SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    expectedType,
                    SyntaxFactory.IdentifierName("IsInstanceOfType")
                ),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("x"))
                    )
                )
            )
        ).WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
            .WithTrailingTrivia(SyntaxFactory.Space));
    }

    private ExpressionSyntax CreateAllItemsAreInstancesOfTypeWithMessage(ExpressionSyntax collection, ExpressionSyntax expectedType, ExpressionSyntax message)
    {
        var result = CreateTUnitAssertionWithMessage("All", collection, message,
            SyntaxFactory.Argument(CreateIsInstanceOfTypeLambda(expectedType)));
        return result;
    }
    
    private ExpressionSyntax? ConvertStringAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // StringAssert message is typically the last parameter after the required args

        return methodName switch
        {
            "Contains" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("Contains", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "Contains" when arguments.Count >= 2 =>
                CreateTUnitAssertion("Contains", arguments[0].Expression, arguments[1]),
            "DoesNotMatch" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("DoesNotMatch", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "DoesNotMatch" when arguments.Count >= 2 =>
                CreateTUnitAssertion("DoesNotMatch", arguments[0].Expression, arguments[1]),
            "EndsWith" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("EndsWith", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "EndsWith" when arguments.Count >= 2 =>
                CreateTUnitAssertion("EndsWith", arguments[0].Expression, arguments[1]),
            "Matches" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("Matches", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "Matches" when arguments.Count >= 2 =>
                CreateTUnitAssertion("Matches", arguments[0].Expression, arguments[1]),
            "StartsWith" when arguments.Count >= 3 =>
                CreateTUnitAssertionWithMessage("StartsWith", arguments[0].Expression, arguments[2].Expression, arguments[1]),
            "StartsWith" when arguments.Count >= 2 =>
                CreateTUnitAssertion("StartsWith", arguments[0].Expression, arguments[1]),
            _ => null
        };
    }

    private ExpressionSyntax? ConvertDirectoryAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // DirectoryAssert.Exists(path) -> Assert.That(Directory.Exists(path)).IsTrue()
        // DirectoryAssert.DoesNotExist(path) -> Assert.That(Directory.Exists(path)).IsFalse()
        // DirectoryAssert.Exists(DirectoryInfo) -> Assert.That(directoryInfo.Exists).IsTrue()

        return methodName switch
        {
            "Exists" when arguments.Count >= 1 => CreateDirectoryExistsAssertion(arguments[0].Expression, isNegated: false),
            "DoesNotExist" when arguments.Count >= 1 => CreateDirectoryExistsAssertion(arguments[0].Expression, isNegated: true),
            _ => null
        };
    }

    private ExpressionSyntax CreateDirectoryExistsAssertion(ExpressionSyntax pathOrDirectoryInfo, bool isNegated)
    {
        // Create: Directory.Exists(path) or directoryInfo.Exists
        ExpressionSyntax existsCheck;

        // If it's a string literal or string variable, use Directory.Exists(path)
        // If it's a DirectoryInfo, use directoryInfo.Exists
        // We'll detect string literals for now and use Directory.Exists
        if (pathOrDirectoryInfo is LiteralExpressionSyntax ||
            pathOrDirectoryInfo.ToString().EndsWith("Path", StringComparison.OrdinalIgnoreCase))
        {
            // Directory.Exists(path)
            existsCheck = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Directory"),
                    SyntaxFactory.IdentifierName("Exists")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(pathOrDirectoryInfo))));
        }
        else
        {
            // Assume it's a DirectoryInfo - use .Exists property
            existsCheck = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                pathOrDirectoryInfo,
                SyntaxFactory.IdentifierName("Exists"));
        }

        var assertionMethod = isNegated ? "IsFalse" : "IsTrue";

        return CreateTUnitAssertion(assertionMethod, existsCheck);
    }

    private ExpressionSyntax? ConvertFileAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // FileAssert.Exists(path) -> Assert.That(File.Exists(path)).IsTrue()
        // FileAssert.DoesNotExist(path) -> Assert.That(File.Exists(path)).IsFalse()

        return methodName switch
        {
            "Exists" when arguments.Count >= 1 => CreateFileExistsAssertion(arguments[0].Expression, isNegated: false),
            "DoesNotExist" when arguments.Count >= 1 => CreateFileExistsAssertion(arguments[0].Expression, isNegated: true),
            _ => null
        };
    }

    private ExpressionSyntax CreateFileExistsAssertion(ExpressionSyntax pathOrFileInfo, bool isNegated)
    {
        // Create: File.Exists(path) or fileInfo.Exists
        ExpressionSyntax existsCheck;

        // If it's a string literal or string variable, use File.Exists(path)
        // If it's a FileInfo, use fileInfo.Exists
        if (pathOrFileInfo is LiteralExpressionSyntax ||
            pathOrFileInfo.ToString().EndsWith("Path", StringComparison.OrdinalIgnoreCase))
        {
            // File.Exists(path)
            existsCheck = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("File"),
                    SyntaxFactory.IdentifierName("Exists")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(pathOrFileInfo))));
        }
        else
        {
            // Assume it's a FileInfo - use .Exists property
            existsCheck = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                pathOrFileInfo,
                SyntaxFactory.IdentifierName("Exists"));
        }

        var assertionMethod = isNegated ? "IsFalse" : "IsTrue";

        return CreateTUnitAssertion(assertionMethod, existsCheck);
    }

    private ExpressionSyntax CreateThrowsAssertion(InvocationExpressionSyntax invocation)
    {
        // Convert Assert.ThrowsException<T>(action) to await Assert.ThrowsAsync<T>(action)
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName)
        {
            var exceptionType = genericName.TypeArgumentList.Arguments[0];
            var action = invocation.ArgumentList.Arguments[0].Expression;

            var invocationExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Assert"),
                    SyntaxFactory.GenericName("ThrowsAsync")
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(exceptionType)
                            )
                        )
                ),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(action)
                    )
                )
            );

            return SyntaxFactory.AwaitExpression(invocationExpression);
        }

        return CreateTUnitAssertion("Throws", invocation.ArgumentList.Arguments[0].Expression);
    }
    
    private ExpressionSyntax CreateThrowsAsyncAssertion(InvocationExpressionSyntax invocation)
    {
        // Similar to CreateThrowsAssertion but for async
        return CreateThrowsAssertion(invocation);
    }
    
    private ExpressionSyntax CreateFailAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // Convert Assert.Fail(message) to await Assert.Fail(message)
        var failInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("Fail")
            ),
            arguments.Count > 0
                ? SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(arguments[0]))
                : SyntaxFactory.ArgumentList()
        );

        return SyntaxFactory.AwaitExpression(failInvocation);
    }
}

public class MSTestBaseTypeRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // MSTest doesn't require specific base classes
        return base.VisitClassDeclaration(node);
    }
}

public class MSTestLifecycleRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Handle ClassInitialize, ClassCleanup, TestInitialize, TestCleanup - remove TestContext parameter where applicable
        var lifecycleAttributes = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => MigrationHelpers.GetAttributeName(a))
            .ToList();

        var hasClassLifecycle = lifecycleAttributes.Any(name => name is "ClassInitialize" or "ClassCleanup");
        var hasTestLifecycle = lifecycleAttributes.Any(name => name is "TestInitialize" or "TestCleanup");

        if (hasClassLifecycle || hasTestLifecycle)
        {
            // Remove TestContext parameter if present
            var parameters = node.ParameterList?.Parameters ?? default;
            if (parameters.Count == 1 && parameters[0].Type?.ToString().Contains("TestContext") == true)
            {
                node = node.WithParameterList(SyntaxFactory.ParameterList());
            }

            // Make sure method is public
            if (!node.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                node = node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }

            // Make sure ClassInitialize/ClassCleanup are static
            if (hasClassLifecycle && !node.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                node = node.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            }
        }

        return base.VisitMethodDeclaration(node);
    }
}

/// <summary>
/// Handles [ExpectedException(typeof(T))] attribute conversion by:
/// 1. Removing the attribute
/// 2. Wrapping the method body in Assert.ThrowsAsync&lt;T&gt;()
/// </summary>
public class MSTestExpectedExceptionRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Find [ExpectedException] attribute
        var expectedExceptionAttr = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => MigrationHelpers.GetAttributeName(a) == "ExpectedException");

        if (expectedExceptionAttr == null)
        {
            return base.VisitMethodDeclaration(node);
        }

        // Extract the exception type from typeof(ExceptionType)
        var exceptionType = ExtractExceptionType(expectedExceptionAttr);
        if (exceptionType == null)
        {
            // Can't extract exception type, leave as-is with a TODO comment
            return node;
        }

        // Remove the [ExpectedException] attribute
        var newAttributeLists = RemoveExpectedExceptionAttribute(node.AttributeLists);

        // Wrap the method body in Assert.ThrowsAsync<T>()
        var newBody = WrapBodyInThrowsAsync(node.Body, node.ExpressionBody, exceptionType);

        if (newBody == null)
        {
            return node.WithAttributeLists(SyntaxFactory.List(newAttributeLists));
        }

        return node
            .WithAttributeLists(SyntaxFactory.List(newAttributeLists))
            .WithBody(newBody)
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    private static TypeSyntax? ExtractExceptionType(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList == null || attribute.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        var firstArg = attribute.ArgumentList.Arguments[0].Expression;

        // Handle typeof(ExceptionType)
        if (firstArg is TypeOfExpressionSyntax typeOfExpr)
        {
            return typeOfExpr.Type;
        }

        return null;
    }

    private static List<AttributeListSyntax> RemoveExpectedExceptionAttribute(SyntaxList<AttributeListSyntax> attributeLists)
    {
        var result = new List<AttributeListSyntax>();

        foreach (var attrList in attributeLists)
        {
            var remainingAttrs = attrList.Attributes
                .Where(a => MigrationHelpers.GetAttributeName(a) != "ExpectedException")
                .ToList();

            if (remainingAttrs.Count > 0)
            {
                result.Add(attrList.WithAttributes(SyntaxFactory.SeparatedList(remainingAttrs)));
            }
        }

        return result;
    }

    private static BlockSyntax? WrapBodyInThrowsAsync(BlockSyntax? body, ArrowExpressionClauseSyntax? expressionBody, TypeSyntax exceptionType)
    {
        StatementSyntax[] originalStatements;

        if (body != null)
        {
            originalStatements = body.Statements.ToArray();
        }
        else if (expressionBody != null)
        {
            // Convert expression body to a statement
            originalStatements = [SyntaxFactory.ExpressionStatement(expressionBody.Expression)];
        }
        else
        {
            return null;
        }

        // Check if the original statements contain any await expressions
        var hasAwait = originalStatements.Any(s => s.DescendantNodes().OfType<AwaitExpressionSyntax>().Any());

        // Create: await Assert.ThrowsAsync<T>(() => { original statements });
        // or: await Assert.ThrowsAsync<T>(async () => { original statements }); if async
        // Add extra indentation for statements inside the lambda block (4 more spaces)
        var indentedStatements = originalStatements.Select(s =>
        {
            var existingTrivia = s.GetLeadingTrivia();
            var newTrivia = existingTrivia.Add(SyntaxFactory.Whitespace("    "));
            return s.WithLeadingTrivia(newTrivia);
        }).ToArray();
        var lambdaBody = SyntaxFactory.Block(indentedStatements)
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                .WithLeadingTrivia(SyntaxFactory.Whitespace("        "))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                .WithLeadingTrivia(SyntaxFactory.Whitespace("        ")));
        ParenthesizedLambdaExpressionSyntax lambda;

        lambda = SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(),
            lambdaBody
        ).WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));

        if (hasAwait)
        {
            // Need async lambda for await expressions
            lambda = lambda.WithAsyncKeyword(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space));
        }

        var throwsAsyncCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.GenericName("ThrowsAsync")
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(exceptionType.WithoutTrivia())
                        )
                    )
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(lambda)
                )
            )
        );

        var awaitExpression = SyntaxFactory.AwaitExpression(throwsAsyncCall);

        var newStatement = SyntaxFactory.ExpressionStatement(awaitExpression)
            .WithLeadingTrivia(SyntaxFactory.TriviaList(
                SyntaxFactory.Whitespace("        ")))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        return SyntaxFactory.Block(newStatement)
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                .WithLeadingTrivia(SyntaxFactory.Whitespace("    ")));
    }
}