using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NUnitMigrationCodeFixProvider)), Shared]
public class NUnitMigrationCodeFixProvider : BaseMigrationCodeFixProvider
{
    protected override string FrameworkName => "NUnit";
    protected override string DiagnosticId => Rules.NUnitMigration.Id;
    protected override string CodeFixTitle => "Convert NUnit code to TUnit";
    
    protected override AttributeRewriter CreateAttributeRewriter(Compilation compilation)
    {
        return new NUnitAttributeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateAssertionRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new NUnitAssertionRewriter(semanticModel);
    }

    protected override CSharpSyntaxRewriter CreateBaseTypeRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new NUnitBaseTypeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateLifecycleRewriter(Compilation compilation)
    {
        return new NUnitLifecycleRewriter();
    }

    protected override CompilationUnitSyntax ApplyFrameworkSpecificConversions(CompilationUnitSyntax compilationUnit, SemanticModel semanticModel, Compilation compilation)
    {
        // Extract TestCase properties FIRST (before ExpectedResult conversion changes the attributes)
        // Maps: TestName → DisplayName, Category → Category, Description/Author → Property, Explicit → Explicit
        var testCasePropertyRewriter = new NUnitTestCasePropertyRewriter();
        compilationUnit = (CompilationUnitSyntax)testCasePropertyRewriter.Visit(compilationUnit);

        // Transform ExpectedResult patterns (TestCase with ExpectedResult → Arguments with assertion)
        var expectedResultRewriter = new NUnitExpectedResultRewriter(semanticModel);
        compilationUnit = (CompilationUnitSyntax)expectedResultRewriter.Visit(compilationUnit);

        // Handle [ExpectedException] attribute conversion
        var expectedExceptionRewriter = new NUnitExpectedExceptionRewriter();
        compilationUnit = (CompilationUnitSyntax)expectedExceptionRewriter.Visit(compilationUnit);

        return compilationUnit;
    }

    /// <summary>
    /// NUnit allows [TestCase] alone, but TUnit requires [Test] + [Arguments].
    /// </summary>
    protected override bool ShouldEnsureTestAttribute() => true;
}

public class NUnitAttributeRewriter : AttributeRewriter
{
    protected override string FrameworkName => "NUnit";
    
    protected override bool IsFrameworkAttribute(string attributeName)
    {
        return attributeName switch
        {
            "Test" or "Theory" or "TestCase" or "TestCaseSource" or
            "SetUp" or "TearDown" or "OneTimeSetUp" or "OneTimeTearDown" or
            "TestFixture" or "Category" or "Ignore" or "Explicit" or "Apartment" or
            "Platform" or "Description" or
            // Parallelization attributes
            "Parallelizable" or "NonParallelizable" or
            // Repeat attribute (same in TUnit)
            "Repeat" or
            // Parameter-level data attributes (converted to Matrix/MatrixRange)
            "Values" or "Range" or "ValueSource" or
            // Combinatorial strategy attributes
            "Sequential" or "Combinatorial" or
            // Exception handling attribute (converted to Assert.ThrowsAsync)
            "ExpectedException" => true,
            _ => false
        };
    }
    
    protected override AttributeSyntax? ConvertAttribute(AttributeSyntax attribute)
    {
        var attributeName = MigrationHelpers.GetAttributeName(attribute);

        // Special handling for [Apartment(ApartmentState.STA)] -> [STAThreadExecutor]
        if (attributeName == "Apartment")
        {
            return ConvertApartmentAttribute(attribute);
        }

        // [Parallelizable(ParallelScope.None)] or [NonParallelizable] -> [NotInParallel]
        if (attributeName is "Parallelizable" or "NonParallelizable")
        {
            return ConvertParallelizableAttribute(attribute, attributeName);
        }

        // [Repeat] has the same name in TUnit - just keep it
        if (attributeName == "Repeat")
        {
            return attribute;
        }

        // [Values] -> [Matrix] (parameter-level attribute)
        if (attributeName == "Values")
        {
            return ConvertValuesAttribute(attribute);
        }

        // [Range] -> [MatrixRange<T>] (parameter-level attribute)
        if (attributeName == "Range")
        {
            return ConvertRangeAttribute(attribute);
        }

        // [ValueSource] -> [MatrixSourceMethod] (parameter-level attribute)
        if (attributeName == "ValueSource")
        {
            return ConvertValueSourceAttribute(attribute);
        }

        // [Combinatorial] - Remove, TUnit's default behavior is combinatorial
        if (attributeName == "Combinatorial")
        {
            return null;
        }

        // [Sequential] - No direct equivalent in TUnit, remove with TODO comment
        // Note: The TODO comment is added in the overridden VisitAttributeList
        if (attributeName == "Sequential")
        {
            return null;
        }

        // [Platform(Include = "Win")] -> [RunOn(OS.Windows)]
        // [Platform(Exclude = "Linux")] -> [ExcludeOn(OS.Linux)]
        if (attributeName == "Platform")
        {
            return ConvertPlatformAttribute(attribute);
        }

        return base.ConvertAttribute(attribute);
    }

    private AttributeSyntax? ConvertApartmentAttribute(AttributeSyntax attribute)
    {
        // Check if the argument is ApartmentState.STA
        if (attribute.ArgumentList?.Arguments.Count > 0)
        {
            var arg = attribute.ArgumentList.Arguments[0].Expression;

            // Check for ApartmentState.STA pattern
            if (arg is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "STA")
            {
                // Convert to [STAThreadExecutor]
                return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("STAThreadExecutor"));
            }
        }

        // For other ApartmentState values, we can't convert automatically - return null to remove
        // Add TODO comment would be ideal but can't easily add comments on attributes
        return null;
    }

    private AttributeSyntax? ConvertParallelizableAttribute(AttributeSyntax attribute, string attributeName)
    {
        // [NonParallelizable] -> [NotInParallel]
        if (attributeName == "NonParallelizable")
        {
            return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("NotInParallel"));
        }

        // [Parallelizable] without arguments means parallel is allowed - no equivalent needed, remove
        if (attribute.ArgumentList == null || attribute.ArgumentList.Arguments.Count == 0)
        {
            return null; // Remove - parallel is the default in TUnit
        }

        // Check the ParallelScope argument
        var arg = attribute.ArgumentList.Arguments[0].Expression;

        // Handle ParallelScope.None -> [NotInParallel]
        if (arg is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "None")
        {
            return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("NotInParallel"));
        }

        // For other ParallelScope values (Self, Children, Fixtures, All), parallel is allowed
        // TUnit's parallel behavior is different - remove the attribute
        return null;
    }

    private AttributeSyntax? ConvertPlatformAttribute(AttributeSyntax attribute)
    {
        // [Platform(Include = "Win")] -> [RunOn(OS.Windows)]
        // [Platform(Exclude = "Linux")] -> [ExcludeOn(OS.Linux)]
        // [Platform("Win")] -> [RunOn(OS.Windows)]

        string? includeValue = null;
        string? excludeValue = null;

        if (attribute.ArgumentList == null || attribute.ArgumentList.Arguments.Count == 0)
        {
            return null; // No arguments, remove the attribute
        }

        foreach (var arg in attribute.ArgumentList.Arguments)
        {
            var argName = arg.NameEquals?.Name.Identifier.Text;
            var value = GetStringLiteralValue(arg.Expression);

            if (argName == "Include" || argName == null)
            {
                // Named argument Include= or positional argument (which is Include)
                includeValue = value;
            }
            else if (argName == "Exclude")
            {
                excludeValue = value;
            }
        }

        // Prefer Include (RunOn) over Exclude (ExcludeOn) if both are present
        if (!string.IsNullOrEmpty(includeValue))
        {
            var osBits = ParsePlatformString(includeValue);
            if (osBits != null)
            {
                return CreateOsAttribute("RunOn", osBits);
            }
        }
        else if (!string.IsNullOrEmpty(excludeValue))
        {
            var osBits = ParsePlatformString(excludeValue);
            if (osBits != null)
            {
                return CreateOsAttribute("ExcludeOn", osBits);
            }
        }

        // Cannot convert - return null to remove the attribute
        return null;
    }

    private static string? GetStringLiteralValue(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
                => literal.Token.ValueText,
            _ => null
        };
    }

    private static List<string>? ParsePlatformString(string? platformString)
    {
        if (string.IsNullOrEmpty(platformString))
        {
            return null;
        }

        var osNames = new List<string>();
        var platforms = platformString.Split(',');

        foreach (var platform in platforms)
        {
            var trimmed = platform.Trim();
            var osName = MapNUnitPlatformToTUnitOS(trimmed);
            if (osName != null && !osNames.Contains(osName))
            {
                osNames.Add(osName);
            }
        }

        return osNames.Count > 0 ? osNames : null;
    }

    private static string? MapNUnitPlatformToTUnitOS(string nunitPlatform)
    {
        // NUnit platform names: https://docs.nunit.org/articles/nunit/writing-tests/attributes/platform.html
        return nunitPlatform.ToLowerInvariant() switch
        {
            "win" or "win32" or "win32s" or "win32nt" or "win32windows" or "wince" or "windows" => "Windows",
            "linux" or "unix" => "Linux",
            "macosx" or "macos" or "osx" or "mac" => "MacOs",
            _ => null // Unknown platform - cannot convert
        };
    }

    private static AttributeSyntax CreateOsAttribute(string attributeName, List<string> osNames)
    {
        // Build OS.Windows | OS.Linux | OS.MacOs expression
        ExpressionSyntax osExpression;

        if (osNames.Count == 1)
        {
            // Single OS: OS.Windows
            osExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("OS"),
                SyntaxFactory.IdentifierName(osNames[0]));
        }
        else
        {
            // Multiple OSes: OS.Windows | OS.Linux
            osExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("OS"),
                SyntaxFactory.IdentifierName(osNames[0]));

            for (int i = 1; i < osNames.Count; i++)
            {
                osExpression = SyntaxFactory.BinaryExpression(
                    SyntaxKind.BitwiseOrExpression,
                    osExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("OS"),
                        SyntaxFactory.IdentifierName(osNames[i])));
            }
        }

        return SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName(attributeName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(osExpression))));
    }

    private AttributeSyntax ConvertValuesAttribute(AttributeSyntax attribute)
    {
        // [Values(1, 2, 3)] -> [Matrix(1, 2, 3)]
        return SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("Matrix"),
            attribute.ArgumentList);
    }

    private AttributeSyntax ConvertRangeAttribute(AttributeSyntax attribute)
    {
        // [Range(1, 10)] -> [MatrixRange<int>(1, 10)]
        // [Range(1.0, 10.0)] -> [MatrixRange<double>(1.0, 10.0)]
        // [Range(1L, 10L)] -> [MatrixRange<long>(1L, 10L)]
        // Detect the type from the first argument
        var rangeType = InferRangeType(attribute.ArgumentList);

        return SyntaxFactory.Attribute(
            SyntaxFactory.GenericName("MatrixRange")
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(rangeType))),
            attribute.ArgumentList);
    }

    private TypeSyntax InferRangeType(AttributeArgumentListSyntax? argumentList)
    {
        if (argumentList == null || argumentList.Arguments.Count == 0)
        {
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
        }

        // Look at the first argument to infer the type
        var firstArg = argumentList.Arguments[0].Expression;

        if (firstArg is LiteralExpressionSyntax literal)
        {
            // Check the token kind and text to determine the type
            var tokenText = literal.Token.Text;

            // Check for explicit suffixes first
            if (tokenText.EndsWith("d", StringComparison.OrdinalIgnoreCase) ||
                tokenText.EndsWith("D", StringComparison.OrdinalIgnoreCase) && !tokenText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
            }
            if (tokenText.EndsWith("f", StringComparison.OrdinalIgnoreCase) ||
                tokenText.EndsWith("F", StringComparison.OrdinalIgnoreCase))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword));
            }
            if (tokenText.EndsWith("m", StringComparison.OrdinalIgnoreCase) ||
                tokenText.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword));
            }
            if (tokenText.EndsWith("L", StringComparison.OrdinalIgnoreCase) ||
                tokenText.EndsWith("l", StringComparison.OrdinalIgnoreCase))
            {
                // Could be long or ulong - check for 'u' prefix on the suffix
                if (tokenText.EndsWith("ul", StringComparison.OrdinalIgnoreCase) ||
                    tokenText.EndsWith("lu", StringComparison.OrdinalIgnoreCase))
                {
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword));
                }
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword));
            }
            if (tokenText.EndsWith("u", StringComparison.OrdinalIgnoreCase) ||
                tokenText.EndsWith("U", StringComparison.OrdinalIgnoreCase))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
            }

            // Check if it contains a decimal point (double by default in C#)
            if (tokenText.Contains('.') || tokenText.Contains('e') || tokenText.Contains('E'))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
            }
        }

        // Check for cast expressions like (byte)1
        if (firstArg is CastExpressionSyntax castExpr)
        {
            return castExpr.Type;
        }

        // Default to int
        return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
    }

    private AttributeSyntax ConvertValueSourceAttribute(AttributeSyntax attribute)
    {
        // [ValueSource(nameof(MyMethod))] -> [MatrixSourceMethod(nameof(MyMethod))]
        // Note: TUnit's MatrixSourceMethod expects a method that returns values for one parameter
        return SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("MatrixSourceMethod"),
            attribute.ArgumentList);
    }
    
    protected override AttributeArgumentListSyntax? ConvertAttributeArguments(AttributeArgumentListSyntax argumentList, string attributeName)
    {
        return attributeName switch
        {
            "TestCase" => ConvertTestCaseArguments(argumentList),
            "TestCaseSource" => ConvertTestCaseSourceArguments(argumentList),
            "Category" => ConvertCategoryArguments(argumentList),
            _ => argumentList
        };
    }

    private AttributeArgumentListSyntax ConvertTestCaseArguments(AttributeArgumentListSyntax argumentList)
    {
        var newArgs = new List<AttributeArgumentSyntax>();
        var categories = new List<ExpressionSyntax>();

        foreach (var arg in argumentList.Arguments)
        {
            var namedProperty = arg.NameEquals?.Name.Identifier.Text;

            if (namedProperty == null)
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
            else if (namedProperty == "TestName")
            {
                // Map NUnit's TestName to TUnit's DisplayName inline on [Arguments]
                var displayNameArg = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("DisplayName")),
                    null,
                    arg.Expression);
                newArgs.Add(displayNameArg);
            }
            else if (namedProperty == "Category")
            {
                // Collect categories to create a Categories array
                categories.Add(arg.Expression);
            }
            else if (namedProperty is "Description" or "Author" or "Explicit" or "ExplicitReason")
            {
                // These properties are converted to separate TUnit attributes by NUnitTestCasePropertyRewriter:
                // Description/Author → [Property], Explicit → [Explicit]
                // Skip them here - they don't belong in the [Arguments] attribute
            }
            else if (namedProperty == "ExpectedResult")
            {
                // ExpectedResult is handled by NUnitExpectedResultRewriter
                // If we get here, it's a case without the ExpectedResult transformation, skip it
            }
            else
            {
                // Other named arguments are preserved as-is
                newArgs.Add(arg);
            }
        }

        // Add Categories array if any categories were found
        if (categories.Count > 0)
        {
            var categoriesArray = SyntaxFactory.CollectionExpression(
                SyntaxFactory.SeparatedList(
                    categories.Select(c => (CollectionElementSyntax)SyntaxFactory.ExpressionElement(c))));

            var categoriesArg = SyntaxFactory.AttributeArgument(
                SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Categories")),
                null,
                categoriesArray);
            newArgs.Add(categoriesArg);
        }

        return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(newArgs));
    }
    
    private AttributeArgumentListSyntax ConvertTestCaseSourceArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert TestCaseSource to MethodDataSource
        if (argumentList.Arguments.Count > 0)
        {
            var firstArg = argumentList.Arguments[0];
            
            // If it's a nameof expression, keep it as is
            if (firstArg.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } })
            {
                return argumentList;
            }
            
            // If it's a string literal, wrap it in quotes if needed
            if (firstArg.Expression is LiteralExpressionSyntax literal)
            {
                return argumentList;
            }
        }
        
        return argumentList;
    }
    
    private AttributeArgumentListSyntax ConvertCategoryArguments(AttributeArgumentListSyntax argumentList)
    {
        // TUnit has a native Category attribute with the same signature as NUnit
        // [Category("Unit")] in NUnit -> [Category("Unit")] in TUnit
        return argumentList;
    }
}

public class NUnitAssertionRewriter : AssertionRewriter
{
    protected override string FrameworkName => "NUnit";

    public NUnitAssertionRewriter(SemanticModel semanticModel) : base(semanticModel)
    {
    }

    /// <summary>
    /// Handles Assert.Multiple(() => { ... }) conversion to using (Assert.Multiple()) { ... }
    /// </summary>
    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        // Check if this is Assert.Multiple(() => { ... })
        if (node.Expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" } &&
            memberAccess.Name.Identifier.Text == "Multiple" &&
            invocation.ArgumentList.Arguments.Count == 1)
        {
            var argument = invocation.ArgumentList.Arguments[0].Expression;

            // Handle lambda: Assert.Multiple(() => { ... })
            if (argument is ParenthesizedLambdaExpressionSyntax lambda)
            {
                return ConvertAssertMultipleLambda(node, lambda);
            }

            // Handle simple lambda: Assert.Multiple(() => expr)
            if (argument is SimpleLambdaExpressionSyntax simpleLambda)
            {
                return ConvertAssertMultipleSimpleLambda(node, simpleLambda);
            }
        }

        return base.VisitExpressionStatement(node);
    }

    private SyntaxNode ConvertAssertMultipleLambda(ExpressionStatementSyntax originalStatement, ParenthesizedLambdaExpressionSyntax lambda)
    {
        // Extract statements from lambda body
        SyntaxList<StatementSyntax> statements;
        if (lambda.Body is BlockSyntax block)
        {
            // Visit each statement to convert inner assertions
            var convertedStatements = block.Statements.Select(s => (StatementSyntax)Visit(s)!).ToArray();
            statements = SyntaxFactory.List(convertedStatements);
        }
        else if (lambda.Body is ExpressionSyntax expr)
        {
            // Single expression lambda - convert it
            var visitedExpr = (ExpressionSyntax)Visit(expr)!;
            statements = SyntaxFactory.SingletonList<StatementSyntax>(
                SyntaxFactory.ExpressionStatement(visitedExpr));
        }
        else
        {
            return originalStatement;
        }

        return CreateUsingMultipleStatement(originalStatement, statements);
    }

    private SyntaxNode ConvertAssertMultipleSimpleLambda(ExpressionStatementSyntax originalStatement, SimpleLambdaExpressionSyntax lambda)
    {
        SyntaxList<StatementSyntax> statements;
        if (lambda.Body is BlockSyntax block)
        {
            var convertedStatements = block.Statements.Select(s => (StatementSyntax)Visit(s)!).ToArray();
            statements = SyntaxFactory.List(convertedStatements);
        }
        else if (lambda.Body is ExpressionSyntax expr)
        {
            var visitedExpr = (ExpressionSyntax)Visit(expr)!;
            statements = SyntaxFactory.SingletonList<StatementSyntax>(
                SyntaxFactory.ExpressionStatement(visitedExpr));
        }
        else
        {
            return originalStatement;
        }

        return CreateUsingMultipleStatement(originalStatement, statements);
    }

    private UsingStatementSyntax CreateUsingMultipleStatement(ExpressionStatementSyntax originalStatement, SyntaxList<StatementSyntax> statements)
    {
        // Create: Assert.Multiple()
        var assertMultipleInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("Multiple")),
            SyntaxFactory.ArgumentList());

        // Create the using statement: using (Assert.Multiple()) { ... }
        var usingStatement = SyntaxFactory.UsingStatement(
            declaration: null,
            expression: assertMultipleInvocation,
            statement: SyntaxFactory.Block(statements)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.LineFeed))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(originalStatement.GetLeadingTrivia())));

        return usingStatement
            .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
            .WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken))
            .WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken))
            .WithLeadingTrivia(originalStatement.GetLeadingTrivia())
            .WithTrailingTrivia(originalStatement.GetTrailingTrivia());
    }

    protected override bool IsFrameworkAssertionNamespace(string namespaceName)
    {
        // Exclude NUnit.Framework.Legacy - ClassicAssert should not be converted
        return (namespaceName == "NUnit.Framework" || namespaceName.StartsWith("NUnit.Framework."))
               && namespaceName != "NUnit.Framework.Legacy";
    }

    protected override bool IsKnownAssertionTypeBySyntax(string targetType, string methodName)
    {
        // NUnit assertion types that can be detected by syntax
        // NOTE: ClassicAssert is NOT included because it's in NUnit.Framework.Legacy namespace
        // and should not be auto-converted. The semantic check excludes it properly.
        return targetType is "Assert" or "CollectionAssert" or "StringAssert" or "FileAssert" or "DirectoryAssert";
    }

    protected override ExpressionSyntax? ConvertAssertionIfNeeded(InvocationExpressionSyntax invocation)
    {
        // Handle FileAssert - check BEFORE IsFrameworkAssertion since FileAssert is a separate class
        if (invocation.Expression is MemberAccessExpressionSyntax fileAccess &&
            fileAccess.Expression is IdentifierNameSyntax { Identifier.Text: "FileAssert" })
        {
            return ConvertFileAssertion(invocation, fileAccess.Name.Identifier.Text);
        }

        // Handle DirectoryAssert - check BEFORE IsFrameworkAssertion since DirectoryAssert is a separate class
        if (invocation.Expression is MemberAccessExpressionSyntax directoryAccess &&
            directoryAccess.Expression is IdentifierNameSyntax { Identifier.Text: "DirectoryAssert" })
        {
            return ConvertDirectoryAssertion(invocation, directoryAccess.Name.Identifier.Text);
        }

        if (!IsFrameworkAssertion(invocation))
        {
            return null;
        }

        // Handle Assert.That(value, constraint)
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "That" &&
            invocation.ArgumentList.Arguments.Count >= 2)
        {
            return ConvertAssertThat(invocation);
        }

        // Handle classic assertions like Assert.AreEqual, ClassicAssert.AreEqual, etc.
        if (invocation.Expression is MemberAccessExpressionSyntax classicMemberAccess &&
            classicMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" or "ClassicAssert" })
        {
            return ConvertClassicAssertion(invocation, classicMemberAccess.Name.Identifier.Text);
        }

        return null;
    }
    
    private ExpressionSyntax ConvertAssertThat(InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList.Arguments;
        var actualValue = arguments[0].Expression;
        var constraint = arguments[1].Expression;

        // Capture the optional message argument (3rd argument)
        ExpressionSyntax? message = null;
        if (arguments.Count >= 3)
        {
            message = arguments[2].Expression;
        }

        // Parse the constraint to determine the TUnit assertion method
        if (constraint is InvocationExpressionSyntax constraintInvocation)
        {
            return ConvertConstraintToTUnitWithMessage(actualValue, constraintInvocation, message);
        }

        if (constraint is MemberAccessExpressionSyntax constraintMember)
        {
            return ConvertConstraintMemberToTUnitWithMessage(actualValue, constraintMember, message);
        }

        return CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint));
    }

    private ExpressionSyntax ConvertConstraintToTUnitWithMessage(ExpressionSyntax actualValue, InvocationExpressionSyntax constraint, ExpressionSyntax? message)
    {
        if (constraint.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Get method name - handle both regular and generic method names
            var methodName = memberAccess.Name switch
            {
                GenericNameSyntax genericName => genericName.Identifier.Text,
                IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
                _ => memberAccess.Name.ToString()
            };

            // Handle chained constraint modifiers like .Within(delta) on Is.EqualTo(x).Within(delta)
            if (methodName == "Within" && memberAccess.Expression is InvocationExpressionSyntax innerConstraint)
            {
                // Get the base assertion (e.g., IsEqualTo(5)) first
                var baseAssertion = ConvertConstraintToTUnitWithMessage(actualValue, innerConstraint, message);
                
                // Now chain .Within(delta) to it
                return ChainMethodCall(baseAssertion, "Within", constraint.ArgumentList.Arguments.ToArray());
            }

            // Handle generic type constraints: Is.TypeOf<T>(), Is.InstanceOf<T>(), Is.AssignableFrom<T>()
            if (memberAccess.Name is GenericNameSyntax genericMethodName)
            {
                var typeArg = genericMethodName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg != null)
                {
                    // Handle Is.Not.TypeOf<T>(), Is.Not.InstanceOf<T>()
                    if (memberAccess.Expression is MemberAccessExpressionSyntax chainedAccessGeneric &&
                        chainedAccessGeneric.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
                        chainedAccessGeneric.Name.Identifier.Text == "Not")
                    {
                        return methodName switch
                        {
                            "TypeOf" => CreateTUnitGenericAssertion("IsNotTypeOf", actualValue, typeArg, message),
                            "InstanceOf" => CreateTUnitGenericAssertion("IsNotAssignableTo", actualValue, typeArg, message),
                            "AssignableFrom" => CreateTUnitGenericAssertion("IsNotAssignableTo", actualValue, typeArg, message),
                            _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
                        };
                    }

                    // Handle Is.TypeOf<T>(), Is.InstanceOf<T>(), Is.AssignableFrom<T>()
                    if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" })
                    {
                        return methodName switch
                        {
                            "TypeOf" => CreateTUnitGenericAssertion("IsTypeOf", actualValue, typeArg, message),
                            "InstanceOf" => CreateTUnitGenericAssertion("IsAssignableTo", actualValue, typeArg, message),
                            "AssignableFrom" => CreateTUnitGenericAssertion("IsAssignableTo", actualValue, typeArg, message),
                            _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
                        };
                    }
                }
            }

            // Handle Is.Not.EqualTo, Is.Not.GreaterThan, etc. (invocation patterns)
            if (memberAccess.Expression is MemberAccessExpressionSyntax chainedAccess &&
                chainedAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
                chainedAccess.Name.Identifier.Text == "Not")
            {
                return methodName switch
                {
                    "EqualTo" => CreateTUnitAssertionWithMessage("IsNotEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThan" => CreateTUnitAssertionWithMessage("IsLessThanOrEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThan" => CreateTUnitAssertionWithMessage("IsGreaterThanOrEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThanOrEqualTo" => CreateTUnitAssertionWithMessage("IsLessThan", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThanOrEqualTo" => CreateTUnitAssertionWithMessage("IsGreaterThan", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "SameAs" => CreateTUnitAssertionWithMessage("IsNotSameReferenceAs", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "InstanceOf" => CreateTUnitAssertionWithMessage("IsNotAssignableTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "TypeOf" => CreateTUnitAssertionWithMessage("IsNotTypeOf", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertionWithMessage("IsNotEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Does.Not.StartWith, Does.Not.EndWith, Does.Not.Contain, Does.Not.Match
            if (memberAccess.Expression is MemberAccessExpressionSyntax doesNotAccess &&
                doesNotAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Does" } &&
                doesNotAccess.Name.Identifier.Text == "Not")
            {
                return methodName switch
                {
                    "StartWith" => CreateTUnitAssertionWithMessage("DoesNotStartWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "EndWith" => CreateTUnitAssertionWithMessage("DoesNotEndWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "Contain" => CreateTUnitAssertionWithMessage("DoesNotContain", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "Match" => CreateTUnitAssertionWithMessage("DoesNotMatch", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertionWithMessage("DoesNotContain", actualValue, message, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Has.Count.EqualTo(n) -> Count().IsEqualTo(n)
            // Pattern: Has.Count is a MemberAccess, then .EqualTo(n) is invoked on it
            if (memberAccess.Expression is MemberAccessExpressionSyntax hasCountAccess &&
                hasCountAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Has" } &&
                hasCountAccess.Name.Identifier.Text == "Count")
            {
                return methodName switch
                {
                    "EqualTo" => CreateCountAssertion(actualValue, "IsEqualTo", message, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThan" => CreateCountAssertion(actualValue, "IsGreaterThan", message, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThan" => CreateCountAssertion(actualValue, "IsLessThan", message, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThanOrEqualTo" => CreateCountAssertion(actualValue, "IsGreaterThanOrEqualTo", message, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThanOrEqualTo" => CreateCountAssertion(actualValue, "IsLessThanOrEqualTo", message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateCountAssertion(actualValue, "IsEqualTo", message, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Has.Member(item) -> Contains(item)
            // Handle Has.Exactly(n) -> will be picked up in member pattern for Has.Exactly(n).Items
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Has" })
            {
                return methodName switch
                {
                    "Member" => CreateTUnitAssertionWithMessage("Contains", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
                };
            }

            // Handle Does.StartWith, Does.EndWith, Does.Match, Contains.Substring
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Does" or "Contains" })
            {
                return methodName switch
                {
                    "StartWith" => CreateTUnitAssertionWithMessage("StartsWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "EndWith" => CreateTUnitAssertionWithMessage("EndsWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "Match" => CreateTUnitAssertionWithMessage("Matches", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "Substring" => CreateTUnitAssertionWithMessage("Contains", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
                };
            }

            return methodName switch
            {
                "EqualTo" => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "GreaterThan" => CreateTUnitAssertionWithMessage("IsGreaterThan", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "LessThan" => CreateTUnitAssertionWithMessage("IsLessThan", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "GreaterThanOrEqualTo" => CreateTUnitAssertionWithMessage("IsGreaterThanOrEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "LessThanOrEqualTo" => CreateTUnitAssertionWithMessage("IsLessThanOrEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "Contains" => CreateTUnitAssertionWithMessage("Contains", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "StartsWith" => CreateTUnitAssertionWithMessage("StartsWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "EndsWith" => CreateTUnitAssertionWithMessage("EndsWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "SameAs" => CreateTUnitAssertionWithMessage("IsSameReferenceAs", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "InstanceOf" => CreateTUnitAssertionWithMessage("IsAssignableTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "TypeOf" => CreateTUnitAssertionWithMessage("IsTypeOf", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "Matches" => CreateTUnitAssertionWithMessage("Matches", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
            };
        }

        return CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint));
    }

    private ExpressionSyntax ConvertConstraintMemberToTUnitWithMessage(ExpressionSyntax actualValue, MemberAccessExpressionSyntax constraint, ExpressionSyntax? message)
    {
        var memberName = constraint.Name.Identifier.Text;

        // Handle Has.Exactly(n).Items -> Count().IsEqualTo(n)
        // Pattern: constraint.Name is "Items", constraint.Expression is Has.Exactly(n) invocation
        if (memberName == "Items" &&
            constraint.Expression is InvocationExpressionSyntax exactlyInvocation &&
            exactlyInvocation.Expression is MemberAccessExpressionSyntax exactlyMemberAccess &&
            exactlyMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Has" } &&
            exactlyMemberAccess.Name.Identifier.Text == "Exactly" &&
            exactlyInvocation.ArgumentList.Arguments.Count > 0)
        {
            // Extract the count argument from Has.Exactly(n)
            var countArg = exactlyInvocation.ArgumentList.Arguments[0];
            return CreateCountAssertion(actualValue, "IsEqualTo", message, countArg);
        }

        // Handle Is.Ordered.Ascending and Is.Ordered.Descending
        if (constraint.Expression is MemberAccessExpressionSyntax orderedMemberAccess &&
            orderedMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
            orderedMemberAccess.Name.Identifier.Text == "Ordered")
        {
            return memberName switch
            {
                "Ascending" => CreateTUnitAssertionWithMessage("IsInAscendingOrder", actualValue, message),
                "Descending" => CreateTUnitAssertionWithMessage("IsInDescendingOrder", actualValue, message),
                _ => CreateTUnitAssertionWithMessage("IsInAscendingOrder", actualValue, message) // Default to ascending for Is.Ordered
            };
        }

        // Handle Is.Not.X patterns (member access, not invocation)
        if (constraint.Expression is MemberAccessExpressionSyntax innerMemberAccess &&
            innerMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
            innerMemberAccess.Name.Identifier.Text == "Not")
        {
            return memberName switch
            {
                "Null" => CreateTUnitAssertionWithMessage("IsNotNull", actualValue, message),
                "Empty" => CreateTUnitAssertionWithMessage("IsNotEmpty", actualValue, message),
                "True" => CreateTUnitAssertionWithMessage("IsFalse", actualValue, message),
                "False" => CreateTUnitAssertionWithMessage("IsTrue", actualValue, message),
                "Positive" => CreateTUnitAssertionWithMessage("IsLessThanOrEqualTo", actualValue, message, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                "Negative" => CreateTUnitAssertionWithMessage("IsGreaterThanOrEqualTo", actualValue, message, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                "Zero" => CreateTUnitAssertionWithMessage("IsNotZero", actualValue, message),
                "NaN" => CreateTUnitAssertionWithMessage("IsNotNaN", actualValue, message),
                _ => CreateTUnitAssertionWithMessage("IsNotEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
            };
        }

        return memberName switch
        {
            "True" => CreateTUnitAssertionWithMessage("IsTrue", actualValue, message),
            "False" => CreateTUnitAssertionWithMessage("IsFalse", actualValue, message),
            "Null" => CreateTUnitAssertionWithMessage("IsNull", actualValue, message),
            "Empty" => CreateTUnitAssertionWithMessage("IsEmpty", actualValue, message),
            "Positive" => CreateTUnitAssertionWithMessage("IsPositive", actualValue, message),
            "Negative" => CreateTUnitAssertionWithMessage("IsNegative", actualValue, message),
            "Zero" => CreateTUnitAssertionWithMessage("IsZero", actualValue, message),
            "NaN" => CreateTUnitAssertionWithMessage("IsNaN", actualValue, message),
            "Unique" => CreateTUnitAssertionWithMessage("HasDistinctItems", actualValue, message),
            "Ordered" => CreateTUnitAssertionWithMessage("IsInAscendingOrder", actualValue, message),
            _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
        };
    }

    private ExpressionSyntax ConvertConstraintToTUnit(ExpressionSyntax actualValue, InvocationExpressionSyntax constraint)
    {
        if (constraint.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Get method name - handle both regular and generic method names
            var methodName = memberAccess.Name switch
            {
                GenericNameSyntax genericName => genericName.Identifier.Text,
                IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
                _ => memberAccess.Name.ToString()
            };

            // Handle chained constraint modifiers like .Within(delta) on Is.EqualTo(x).Within(delta)
            if (methodName == "Within" && memberAccess.Expression is InvocationExpressionSyntax innerConstraint)
            {
                // Get the base assertion (e.g., IsEqualTo(5)) first
                var baseAssertion = ConvertConstraintToTUnit(actualValue, innerConstraint);
                
                // Now chain .Within(delta) to it
                return ChainMethodCall(baseAssertion, "Within", constraint.ArgumentList.Arguments.ToArray());
            }

            // Handle generic type constraints: Is.TypeOf<T>(), Is.InstanceOf<T>(), Is.AssignableFrom<T>()
            if (memberAccess.Name is GenericNameSyntax genericMethodName)
            {
                var typeArg = genericMethodName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg != null)
                {
                    // Handle Is.Not.TypeOf<T>(), Is.Not.InstanceOf<T>()
                    if (memberAccess.Expression is MemberAccessExpressionSyntax chainedAccessGeneric &&
                        chainedAccessGeneric.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
                        chainedAccessGeneric.Name.Identifier.Text == "Not")
                    {
                        return methodName switch
                        {
                            "TypeOf" => CreateTUnitGenericAssertion("IsNotTypeOf", actualValue, typeArg, null),
                            "InstanceOf" => CreateTUnitGenericAssertion("IsNotAssignableTo", actualValue, typeArg, null),
                            "AssignableFrom" => CreateTUnitGenericAssertion("IsNotAssignableTo", actualValue, typeArg, null),
                            _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
                        };
                    }

                    // Handle Is.TypeOf<T>(), Is.InstanceOf<T>(), Is.AssignableFrom<T>()
                    if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" })
                    {
                        return methodName switch
                        {
                            "TypeOf" => CreateTUnitGenericAssertion("IsTypeOf", actualValue, typeArg, null),
                            "InstanceOf" => CreateTUnitGenericAssertion("IsAssignableTo", actualValue, typeArg, null),
                            "AssignableFrom" => CreateTUnitGenericAssertion("IsAssignableTo", actualValue, typeArg, null),
                            _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
                        };
                    }
                }
            }

            // Handle Is.Not.EqualTo, Is.Not.GreaterThan, etc. (invocation patterns)
            if (memberAccess.Expression is MemberAccessExpressionSyntax chainedAccess &&
                chainedAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
                chainedAccess.Name.Identifier.Text == "Not")
            {
                return methodName switch
                {
                    "EqualTo" => CreateTUnitAssertion("IsNotEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThan" => CreateTUnitAssertion("IsLessThanOrEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThan" => CreateTUnitAssertion("IsGreaterThanOrEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThanOrEqualTo" => CreateTUnitAssertion("IsLessThan", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThanOrEqualTo" => CreateTUnitAssertion("IsGreaterThan", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "SameAs" => CreateTUnitAssertion("IsNotSameReferenceAs", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "InstanceOf" => CreateTUnitAssertion("IsNotAssignableTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "TypeOf" => CreateTUnitAssertion("IsNotTypeOf", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertion("IsNotEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Does.Not.StartWith, Does.Not.EndWith, Does.Not.Contain, Does.Not.Match
            if (memberAccess.Expression is MemberAccessExpressionSyntax doesNotAccess &&
                doesNotAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Does" } &&
                doesNotAccess.Name.Identifier.Text == "Not")
            {
                return methodName switch
                {
                    "StartWith" => CreateTUnitAssertion("DoesNotStartWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "EndWith" => CreateTUnitAssertion("DoesNotEndWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "Contain" => CreateTUnitAssertion("DoesNotContain", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "Match" => CreateTUnitAssertion("DoesNotMatch", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertion("DoesNotContain", actualValue, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Has.Member(item) -> Contains(item)
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Has" })
            {
                return methodName switch
                {
                    "Member" => CreateTUnitAssertion("Contains", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
                };
            }

            // Handle Does.StartWith, Does.EndWith, Does.Match, Contains.Substring
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Does" or "Contains" })
            {
                return methodName switch
                {
                    "StartWith" => CreateTUnitAssertion("StartsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "EndWith" => CreateTUnitAssertion("EndsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "Match" => CreateTUnitAssertion("Matches", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "Substring" => CreateTUnitAssertion("Contains", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
                };
            }

            return methodName switch
            {
                "EqualTo" => CreateTUnitAssertion("IsEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "GreaterThan" => CreateTUnitAssertion("IsGreaterThan", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "LessThan" => CreateTUnitAssertion("IsLessThan", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "GreaterThanOrEqualTo" => CreateTUnitAssertion("IsGreaterThanOrEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "LessThanOrEqualTo" => CreateTUnitAssertion("IsLessThanOrEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "Contains" => CreateTUnitAssertion("Contains", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "StartsWith" => CreateTUnitAssertion("StartsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "EndsWith" => CreateTUnitAssertion("EndsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "SameAs" => CreateTUnitAssertion("IsSameReferenceAs", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "InstanceOf" => CreateTUnitAssertion("IsAssignableTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "TypeOf" => CreateTUnitAssertion("IsTypeOf", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "SubsetOf" => CreateTUnitAssertion("IsSubsetOf", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "SupersetOf" => CreateTUnitAssertion("IsSupersetOf", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "EquivalentTo" => CreateTUnitAssertion("IsEquivalentTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "Matches" => CreateTUnitAssertion("Matches", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "InRange" => CreateInRangeAssertion(actualValue, constraint.ArgumentList.Arguments),
                _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
            };
        }

        return CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint));
    }
    
    private ExpressionSyntax ConvertConstraintMemberToTUnit(ExpressionSyntax actualValue, MemberAccessExpressionSyntax constraint)
    {
        var memberName = constraint.Name.Identifier.Text;

        // Handle Has.Exactly(n).Items -> Count().IsEqualTo(n)
        // Pattern: constraint.Name is "Items", constraint.Expression is Has.Exactly(n) invocation
        if (memberName == "Items" &&
            constraint.Expression is InvocationExpressionSyntax exactlyInvocation &&
            exactlyInvocation.Expression is MemberAccessExpressionSyntax exactlyMemberAccess &&
            exactlyMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Has" } &&
            exactlyMemberAccess.Name.Identifier.Text == "Exactly" &&
            exactlyInvocation.ArgumentList.Arguments.Count > 0)
        {
            // Extract the count argument from Has.Exactly(n)
            var countArg = exactlyInvocation.ArgumentList.Arguments[0];
            return CreateCountAssertion(actualValue, "IsEqualTo", null, countArg);
        }

        // Handle Is.Ordered.Ascending and Is.Ordered.Descending
        if (constraint.Expression is MemberAccessExpressionSyntax orderedMemberAccess &&
            orderedMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
            orderedMemberAccess.Name.Identifier.Text == "Ordered")
        {
            return memberName switch
            {
                "Ascending" => CreateTUnitAssertion("IsInAscendingOrder", actualValue),
                "Descending" => CreateTUnitAssertion("IsInDescendingOrder", actualValue),
                _ => CreateTUnitAssertion("IsInAscendingOrder", actualValue) // Default to ascending for Is.Ordered
            };
        }

        // Handle Is.Not.X patterns (member access, not invocation)
        if (constraint.Expression is MemberAccessExpressionSyntax innerMemberAccess &&
            innerMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
            innerMemberAccess.Name.Identifier.Text == "Not")
        {
            return memberName switch
            {
                "Null" => CreateTUnitAssertion("IsNotNull", actualValue),
                "Empty" => CreateTUnitAssertion("IsNotEmpty", actualValue),
                "True" => CreateTUnitAssertion("IsFalse", actualValue),
                "False" => CreateTUnitAssertion("IsTrue", actualValue),
                "Positive" => CreateTUnitAssertion("IsLessThanOrEqualTo", actualValue, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                "Negative" => CreateTUnitAssertion("IsGreaterThanOrEqualTo", actualValue, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                "Zero" => CreateTUnitAssertion("IsNotZero", actualValue),
                "NaN" => CreateTUnitAssertion("IsNotNaN", actualValue),
                _ => CreateTUnitAssertion("IsNotEqualTo", actualValue, SyntaxFactory.Argument(constraint))
            };
        }

        return memberName switch
        {
            "True" => CreateTUnitAssertion("IsTrue", actualValue),
            "False" => CreateTUnitAssertion("IsFalse", actualValue),
            "Null" => CreateTUnitAssertion("IsNull", actualValue),
            "Empty" => CreateTUnitAssertion("IsEmpty", actualValue),
            "Positive" => CreateTUnitAssertion("IsPositive", actualValue),
            "Negative" => CreateTUnitAssertion("IsNegative", actualValue),
            "Zero" => CreateTUnitAssertion("IsZero", actualValue),
            "NaN" => CreateTUnitAssertion("IsNaN", actualValue),
            "Unique" => CreateTUnitAssertion("HasDistinctItems", actualValue),
            "Ordered" => CreateTUnitAssertion("IsInAscendingOrder", actualValue),
            _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
        };
    }

    private ExpressionSyntax CreateInRangeAssertion(ExpressionSyntax actualValue, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // Is.InRange(low, high) -> IsInRange(low, high)
        if (arguments.Count >= 2)
        {
            return CreateTUnitAssertion("IsInRange", actualValue, arguments[0], arguments[1]);
        }
        return CreateTUnitAssertion("IsInRange", actualValue);
    }

    /// <summary>
    /// Creates a count-based assertion: await Assert.That(collection).Count().IsEqualTo(n)
    /// Used for Has.Count.EqualTo(n) and Has.Exactly(n).Items patterns
    /// </summary>
    private ExpressionSyntax CreateCountAssertion(ExpressionSyntax actualValue, string comparisonMethod, ExpressionSyntax? message, params ArgumentSyntax[] arguments)
    {
        // Create Assert.That(collection)
        var assertThatInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("That")
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(actualValue)
                )
            )
        );

        // Create Assert.That(collection).Count()
        var countInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                assertThatInvocation,
                SyntaxFactory.IdentifierName("Count")
            ),
            SyntaxFactory.ArgumentList()
        );

        // Create Assert.That(collection).Count().IsEqualTo(n) (or other comparison method)
        var comparisonAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            countInvocation,
            SyntaxFactory.IdentifierName(comparisonMethod)
        );

        var comparisonArgs = arguments.Length > 0
            ? SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))
            : SyntaxFactory.ArgumentList();

        ExpressionSyntax fullInvocation = SyntaxFactory.InvocationExpression(comparisonAccess, comparisonArgs);

        // Add .Because(message) if message is provided
        if (message != null)
        {
            var becauseAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                fullInvocation,
                SyntaxFactory.IdentifierName("Because")
            );

            fullInvocation = SyntaxFactory.InvocationExpression(
                becauseAccess,
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(message)
                    )
                )
            );
        }

        // Wrap in await or .Wait() depending on whether the method can be async
        return WrapAssertionForAsync(fullInvocation);
    }

    /// <summary>
    /// Chains a method call onto an existing await expression or .Wait() expression.
    /// For example: await Assert.That(x).IsEqualTo(5) becomes await Assert.That(x).IsEqualTo(5).Within(2)
    /// </summary>
    private ExpressionSyntax ChainMethodCall(ExpressionSyntax baseExpression, string methodName, params ArgumentSyntax[] arguments)
    {
        ExpressionSyntax innerInvocation;

        // The base expression is either:
        // 1. An AwaitExpression like: await Assert.That(x).IsEqualTo(5)
        // 2. An InvocationExpression like: Assert.That(x).IsEqualTo(5).Wait() (for ref/out methods)
        if (baseExpression is AwaitExpressionSyntax awaitExpr)
        {
            innerInvocation = awaitExpr.Expression;
        }
        else if (baseExpression is InvocationExpressionSyntax waitInvocation &&
                 waitInvocation.Expression is MemberAccessExpressionSyntax waitAccess &&
                 waitAccess.Name.Identifier.Text == "Wait")
        {
            // Extract the expression before .Wait()
            innerInvocation = waitAccess.Expression;
        }
        else
        {
            // Fallback: just return the base expression if it's not the expected shape
            return baseExpression;
        }

        // Create the chained method access: Assert.That(x).IsEqualTo(5).Within
        var chainedAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            innerInvocation,
            SyntaxFactory.IdentifierName(methodName)
        );

        // Create the invocation: Assert.That(x).IsEqualTo(5).Within(2)
        var chainedInvocation = SyntaxFactory.InvocationExpression(
            chainedAccess,
            arguments.Length > 0
                ? SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))
                : SyntaxFactory.ArgumentList()
        );

        // Re-wrap in await or .Wait() depending on method context
        return WrapAssertionForAsync(chainedInvocation);
    }
    
    private ExpressionSyntax? ConvertClassicAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // Handle Assert.Throws<T> and Assert.ThrowsAsync<T> first (generic methods)
        if (methodName is "Throws" or "ThrowsAsync")
        {
            return ConvertNUnitThrows(invocation);
        }

        // Handle Assert.DoesNotThrow and Assert.DoesNotThrowAsync
        if (methodName is "DoesNotThrow" or "DoesNotThrowAsync")
        {
            return ConvertDoesNotThrow(arguments);
        }

        // Handle special assertions (Pass, Inconclusive, Fail, Warn)
        return methodName switch
        {
            // Pass and Fail
            "Pass" => CreatePassAssertion(arguments),
            "Fail" => CreateFailAssertion(arguments),
            "Inconclusive" => CreateSkipAssertion(arguments),
            "Ignore" => CreateSkipAssertion(arguments),
            "Warn" => CreateWarnAssertion(arguments),

            // 2-arg assertions (expected, actual) with optional message at index 2
            "AreEqual" when arguments.Count >= 2 => ConvertAreEqualWithComparer(arguments),
            "AreNotEqual" when arguments.Count >= 2 => ConvertAreNotEqualWithMessage(arguments),
            "AreSame" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsSameReferenceAs", arguments),
            "AreNotSame" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsNotSameReferenceAs", arguments),
            "Greater" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsGreaterThan", arguments, swapArgs: false),
            "GreaterOrEqual" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsGreaterThanOrEqualTo", arguments, swapArgs: false),
            "Less" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsLessThan", arguments, swapArgs: false),
            "LessOrEqual" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsLessThanOrEqualTo", arguments, swapArgs: false),

            // 1-arg assertions with optional message at index 1
            "IsTrue" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsTrue", arguments),
            "IsFalse" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsFalse", arguments),
            "IsNull" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNull", arguments),
            "IsNotNull" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNotNull", arguments),
            "IsEmpty" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsEmpty", arguments),
            "IsNotEmpty" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNotEmpty", arguments),
            "IsNaN" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNaN", arguments),
            "IsInstanceOf" when arguments.Count >= 2 => ConvertInstanceOf(arguments, isNegated: false),
            "IsNotInstanceOf" when arguments.Count >= 2 => ConvertInstanceOf(arguments, isNegated: true),

            // Collection assertions
            "Contains" when arguments.Count >= 2 => ConvertTwoArgWithMessage("Contains", arguments, swapArgs: false),

            // Comparison assertions
            "Positive" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsPositive", arguments),
            "Negative" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNegative", arguments),
            "Zero" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsZero", arguments),
            "NotZero" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNotZero", arguments),

            _ => null
        };
    }

    private ExpressionSyntax ConvertOneArgWithMessage(string methodName, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var actualValue = arguments[0].Expression;
        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 1);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage(methodName, actualValue, messageExpr);
    }

    private ExpressionSyntax ConvertTwoArgWithMessage(string methodName, SeparatedSyntaxList<ArgumentSyntax> arguments, bool swapArgs = true)
    {
        // For most NUnit assertions: expected is first, actual is second
        // For TUnit: actual is first, expected goes in the method call
        var actualValue = swapArgs ? arguments[1].Expression : arguments[0].Expression;
        var expectedArg = swapArgs ? arguments[0] : arguments[1];
        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 2);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage(methodName, actualValue, messageExpr, expectedArg);
    }

    private ExpressionSyntax ConvertAreEqualWithComparer(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var expected = arguments[0];
        var actual = arguments[1].Expression;

        // Check if 3rd argument is a comparer (not a string message)
        if (arguments.Count >= 3 && IsLikelyComparerArgument(arguments[2]))
        {
            // Add TODO comment and skip the comparer
            var result = CreateTUnitAssertion("IsEqualTo", actual, expected);
            return result.WithLeadingTrivia(
                CreateTodoComment("custom comparer was used - consider using Assert.That(...).IsEquivalentTo() or a custom condition."),
                SyntaxFactory.EndOfLine("\n"),
                SyntaxFactory.Whitespace("                "));
        }

        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 2);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage("IsEqualTo", actual, messageExpr, expected);
    }

    private ExpressionSyntax ConvertAreNotEqualWithMessage(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var expected = arguments[0];
        var actual = arguments[1].Expression;

        // Check if 3rd argument is a comparer
        if (arguments.Count >= 3 && IsLikelyComparerArgument(arguments[2]))
        {
            var result = CreateTUnitAssertion("IsNotEqualTo", actual, expected);
            return result.WithLeadingTrivia(
                CreateTodoComment("custom comparer was used - consider using a custom condition."),
                SyntaxFactory.EndOfLine("\n"),
                SyntaxFactory.Whitespace("                "));
        }

        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 2);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage("IsNotEqualTo", actual, messageExpr, expected);
    }

    private ExpressionSyntax ConvertInstanceOf(SeparatedSyntaxList<ArgumentSyntax> arguments, bool isNegated)
    {
        var actualValue = arguments[0].Expression;
        var expectedType = arguments[1];
        var methodName = isNegated ? "IsNotAssignableTo" : "IsAssignableTo";
        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 2);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage(methodName, actualValue, messageExpr, expectedType);
    }

    private ExpressionSyntax ConvertNUnitThrows(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Handle generic form: Assert.Throws<T>(() => ...) or Assert.ThrowsAsync<T>(() => ...)
            if (memberAccess.Name is GenericNameSyntax genericName)
            {
                var exceptionType = genericName.TypeArgumentList.Arguments[0];
                var action = invocation.ArgumentList.Arguments[0].Expression;

                var throwsAsyncInvocation = SyntaxFactory.InvocationExpression(
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

                return WrapAssertionForAsync(throwsAsyncInvocation);
            }

            // Handle non-generic constraint-based form: Assert.Throws(constraint, () => ...) or Assert.ThrowsAsync(constraint, () => ...)
            // where constraint is typically Is.TypeOf(typeof(T))
            if (invocation.ArgumentList.Arguments.Count >= 2)
            {
                var constraint = invocation.ArgumentList.Arguments[0].Expression;
                var action = invocation.ArgumentList.Arguments[1].Expression;

                // Try to extract the exception type from the constraint
                var exceptionType = TryExtractTypeFromConstraint(constraint);

                if (exceptionType != null)
                {
                    // Convert to generic ThrowsAsync form: Assert.ThrowsAsync<T>(() => ...)
                    var throwsAsyncInvocation = SyntaxFactory.InvocationExpression(
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

                    return WrapAssertionForAsync(throwsAsyncInvocation);
                }
            }
        }

        // Fallback for unsupported Throws patterns
        // If we have 2+ arguments, it's a constraint-based form where arg[1] is the action
        // Otherwise, it's a single-argument form where arg[0] is the action
        var fallbackArg = invocation.ArgumentList.Arguments.Count >= 2
            ? invocation.ArgumentList.Arguments[1].Expression
            : invocation.ArgumentList.Arguments[0].Expression;
        return CreateTUnitAssertion("Throws", fallbackArg);
    }

    private ExpressionSyntax ConvertDoesNotThrow(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // Assert.DoesNotThrow(() => action) -> await Assert.That(() => action).ThrowsNothing()
        // Use the action from arguments, or a no-op lambda as fallback
        var action = arguments.Count > 0
            ? arguments[0].Expression
            : SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.Block());
        
        // Create Assert.That(() => action)
        var assertThatInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("That")
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(action)
                )
            )
        );
        
        // Chain .ThrowsNothing()
        var throwsNothingInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                assertThatInvocation,
                SyntaxFactory.IdentifierName("ThrowsNothing")
            ),
            SyntaxFactory.ArgumentList()
        );
        
        // Wrap in await or .Wait() depending on method context
        return WrapAssertionForAsync(throwsNothingInvocation);
    }

    /// <summary>
    /// Attempts to extract the exception type from NUnit constraint expressions like Is.TypeOf(typeof(T)).
    /// Returns null if the type cannot be extracted.
    /// </summary>
    private TypeSyntax? TryExtractTypeFromConstraint(ExpressionSyntax constraint)
    {
        // Handle Is.TypeOf(typeof(T)) pattern
        if (constraint is InvocationExpressionSyntax invocation)
        {
            // Check if it's a method call like Is.TypeOf(...) or TypeOf(...)
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "TypeOf" &&
                invocation.ArgumentList.Arguments.Count > 0)
            {
                // Extract the argument to TypeOf - should be typeof(T)
                var typeofArg = invocation.ArgumentList.Arguments[0].Expression;
                return ExtractTypeFromTypeof(typeofArg);
            }
            
            // Handle standalone TypeOf(typeof(T)) calls
            if (invocation.Expression is IdentifierNameSyntax { Identifier.Text: "TypeOf" } &&
                invocation.ArgumentList.Arguments.Count > 0)
            {
                var typeofArg = invocation.ArgumentList.Arguments[0].Expression;
                return ExtractTypeFromTypeof(typeofArg);
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Extracts the type from a typeof(T) expression.
    /// </summary>
    private TypeSyntax? ExtractTypeFromTypeof(ExpressionSyntax expression)
    {
        if (expression is TypeOfExpressionSyntax typeofExpression)
        {
            return typeofExpression.Type;
        }
        
        return null;
    }

    private ExpressionSyntax CreatePassAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var passInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("Pass")
            ),
            arguments.Count > 0
                ? SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(arguments[0]))
                : SyntaxFactory.ArgumentList()
        );

        return WrapAssertionForAsync(passInvocation);
    }

    private ExpressionSyntax CreateFailAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // TUnit: Fail.Test("reason") - not awaited, throws synchronously
        var reasonArg = arguments.Count > 0
            ? arguments[0]
            : SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Test failed")));

        var failInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Fail"),
                SyntaxFactory.IdentifierName("Test")
            ),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(reasonArg))
        );

        return failInvocation;
    }

    private ExpressionSyntax CreateSkipAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // TUnit: Skip.Test("reason") - not awaited, throws SkipTestException
        var reasonArg = arguments.Count > 0
            ? arguments[0]
            : SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Test skipped")));

        var skipInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Skip"),
                SyntaxFactory.IdentifierName("Test")
            ),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(reasonArg))
        );

        return skipInvocation;
    }

    private ExpressionSyntax CreateWarnAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // NUnit's Assert.Warn outputs a warning but allows the test to continue
        // TUnit doesn't have a direct equivalent, so we convert to Skip.Test with a prefix
        // Users may want to handle warnings differently (e.g., console output)
        var messageArg = arguments.Count > 0
            ? arguments[0]
            : SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Warning")));

        // Wrap the message in "Warning: " prefix
        var prefixedMessage = SyntaxFactory.Argument(
            SyntaxFactory.InterpolatedStringExpression(
                SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
                SyntaxFactory.List<InterpolatedStringContentSyntax>(
                    [
                        SyntaxFactory.InterpolatedStringText()
                            .WithTextToken(SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(),
                                SyntaxKind.InterpolatedStringTextToken,
                                "Warning: ",
                                "Warning: ",
                                SyntaxFactory.TriviaList())),
                        SyntaxFactory.Interpolation(messageArg.Expression)
                    ])));

        var skipInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Skip"),
                SyntaxFactory.IdentifierName("Test")
            ),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(prefixedMessage))
        );

        return skipInvocation;
    }

    private ExpressionSyntax? ConvertFileAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // FileAssert.Exists(path) -> Assert.That(File.Exists(path)).IsTrue()
        // FileAssert.DoesNotExist(path) -> Assert.That(File.Exists(path)).IsFalse()
        // FileAssert.AreEqual(expected, actual) -> Assert.That(File.ReadAllBytes(actual)).IsEquivalentTo(File.ReadAllBytes(expected))
        // FileAssert.AreNotEqual(expected, actual) -> Assert.That(File.ReadAllBytes(actual)).IsNotEquivalentTo(File.ReadAllBytes(expected))

        return methodName switch
        {
            "Exists" when arguments.Count >= 1 => CreateFileExistsAssertion(arguments[0].Expression, isNegated: false),
            "DoesNotExist" when arguments.Count >= 1 => CreateFileExistsAssertion(arguments[0].Expression, isNegated: true),
            "AreEqual" when arguments.Count >= 2 => CreateFileAreEqualAssertion(arguments[0].Expression, arguments[1].Expression, isNegated: false),
            "AreNotEqual" when arguments.Count >= 2 => CreateFileAreEqualAssertion(arguments[0].Expression, arguments[1].Expression, isNegated: true),
            _ => null
        };
    }

    private ExpressionSyntax CreateFileExistsAssertion(ExpressionSyntax pathOrFileInfo, bool isNegated)
    {
        // Create: File.Exists(path) or fileInfo.Exists
        ExpressionSyntax existsCheck;

        // If it's a string literal or string-like expression, use File.Exists(path)
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

    private ExpressionSyntax CreateFileAreEqualAssertion(ExpressionSyntax expected, ExpressionSyntax actual, bool isNegated)
    {
        // NUnit's FileAssert.AreEqual compares file contents
        // Generate: Assert.That(new FileInfo(actual)).HasSameContentAs(new FileInfo(expected))
        // Or for negated: Assert.That(new FileInfo(actual)).DoesNotHaveSameContentAs(new FileInfo(expected))

        var actualFileInfo = CreateFileInfoExpression(actual);
        var expectedFileInfo = CreateFileInfoExpression(expected);

        var assertionMethod = isNegated ? "DoesNotHaveSameContentAs" : "HasSameContentAs";
        return CreateTUnitAssertion(assertionMethod, actualFileInfo, SyntaxFactory.Argument(expectedFileInfo));
    }

    private static ExpressionSyntax CreateFileInfoExpression(ExpressionSyntax pathOrFileInfo)
    {
        // If it's already a FileInfo (not a string path), use it directly
        // If the expression is a string literal or looks like a path variable, wrap it
        if (pathOrFileInfo is LiteralExpressionSyntax ||
            pathOrFileInfo.ToString().EndsWith("Path", StringComparison.OrdinalIgnoreCase) ||
            pathOrFileInfo.ToString().Contains("path", StringComparison.OrdinalIgnoreCase))
        {
            // Create: new FileInfo(path)
            return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName("FileInfo"))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(pathOrFileInfo))));
        }

        // Assume it's already a FileInfo or can be used as-is
        return pathOrFileInfo;
    }

    private static ExpressionSyntax CreateFileReadAllBytes(ExpressionSyntax pathOrFileInfo)
    {
        // If it's a FileInfo, use fileInfo.FullName
        ExpressionSyntax path;
        if (pathOrFileInfo is LiteralExpressionSyntax ||
            pathOrFileInfo.ToString().EndsWith("Path", StringComparison.OrdinalIgnoreCase))
        {
            path = pathOrFileInfo;
        }
        else
        {
            // Assume it's a FileInfo - use .FullName property
            path = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                pathOrFileInfo,
                SyntaxFactory.IdentifierName("FullName"));
        }

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("File"),
                SyntaxFactory.IdentifierName("ReadAllBytes")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(path))));
    }

    private ExpressionSyntax? ConvertDirectoryAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // DirectoryAssert.Exists(path) -> Assert.That(Directory.Exists(path)).IsTrue()
        // DirectoryAssert.DoesNotExist(path) -> Assert.That(Directory.Exists(path)).IsFalse()
        // DirectoryAssert.AreEqual(expected, actual) -> Assert.That(Directory.GetFiles(actual)).IsEquivalentTo(Directory.GetFiles(expected))
        // DirectoryAssert.AreNotEqual(expected, actual) -> Assert.That(Directory.GetFiles(actual)).IsNotEquivalentTo(Directory.GetFiles(expected))

        return methodName switch
        {
            "Exists" when arguments.Count >= 1 => CreateDirectoryExistsAssertion(arguments[0].Expression, isNegated: false),
            "DoesNotExist" when arguments.Count >= 1 => CreateDirectoryExistsAssertion(arguments[0].Expression, isNegated: true),
            "AreEqual" when arguments.Count >= 2 => CreateDirectoryAreEqualAssertion(arguments[0].Expression, arguments[1].Expression, isNegated: false),
            "AreNotEqual" when arguments.Count >= 2 => CreateDirectoryAreEqualAssertion(arguments[0].Expression, arguments[1].Expression, isNegated: true),
            _ => null
        };
    }

    private ExpressionSyntax CreateDirectoryExistsAssertion(ExpressionSyntax pathOrDirectoryInfo, bool isNegated)
    {
        // Create: Directory.Exists(path) or directoryInfo.Exists
        ExpressionSyntax existsCheck;

        // If it's a string literal or string-like expression, use Directory.Exists(path)
        // If it's a DirectoryInfo, use directoryInfo.Exists
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

    private ExpressionSyntax CreateDirectoryAreEqualAssertion(ExpressionSyntax expected, ExpressionSyntax actual, bool isNegated)
    {
        // NUnit's DirectoryAssert.AreEqual compares both file structure AND file contents
        // Generate: Assert.That(new DirectoryInfo(actual)).IsEquivalentTo(new DirectoryInfo(expected))

        var actualDirectoryInfo = CreateDirectoryInfoExpression(actual);
        var expectedDirectoryInfo = CreateDirectoryInfoExpression(expected);

        var assertionMethod = isNegated ? "IsNotEquivalentTo" : "IsEquivalentTo";
        return CreateTUnitAssertion(assertionMethod, actualDirectoryInfo, SyntaxFactory.Argument(expectedDirectoryInfo));
    }

    private static ExpressionSyntax CreateDirectoryInfoExpression(ExpressionSyntax pathOrDirectoryInfo)
    {
        // If it's already a DirectoryInfo (not a string path), use it directly
        // We can't easily determine the type at syntax level, so we wrap in new DirectoryInfo() for string paths
        // If the expression is a string literal or looks like a path variable, wrap it
        if (pathOrDirectoryInfo is LiteralExpressionSyntax ||
            pathOrDirectoryInfo.ToString().EndsWith("Path", StringComparison.OrdinalIgnoreCase) ||
            pathOrDirectoryInfo.ToString().Contains("path", StringComparison.OrdinalIgnoreCase))
        {
            // Create: new DirectoryInfo(path)
            return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName("DirectoryInfo"))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(pathOrDirectoryInfo))));
        }

        // Assume it's already a DirectoryInfo or can be used as-is
        return pathOrDirectoryInfo;
    }

}

public class NUnitBaseTypeRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // NUnit doesn't require specific base classes, but might have IDisposable for cleanup
        // For now, just return the node as is
        return base.VisitClassDeclaration(node);
    }
}

public class NUnitLifecycleRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Lifecycle methods are handled by attribute conversion
        // Just ensure they're public and have correct signature
        var hasLifecycleAttribute = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Before" or "After");

        if (hasLifecycleAttribute && !node.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }

        return base.VisitMethodDeclaration(node);
    }
}

/// <summary>
/// Handles NUnit [ExpectedException(typeof(T))] attribute conversion by:
/// 1. Removing the attribute
/// 2. Wrapping the method body in Assert.ThrowsAsync&lt;T&gt;()
/// </summary>
public class NUnitExpectedExceptionRewriter : CSharpSyntaxRewriter
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