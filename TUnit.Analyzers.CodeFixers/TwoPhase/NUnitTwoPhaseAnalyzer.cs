using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base.TwoPhase;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers.CodeFixers.TwoPhase;

/// <summary>
/// Phase 1 analyzer for NUnit to TUnit migration.
/// Collects all conversion targets while the semantic model is valid.
/// </summary>
public class NUnitTwoPhaseAnalyzer : MigrationAnalyzer
{
    private static readonly HashSet<string> NUnitClassicAssertMethods = new()
    {
        "AreEqual", "AreNotEqual", "AreSame", "AreNotSame",
        "IsTrue", "IsFalse",
        "IsNull", "IsNotNull",
        "IsEmpty", "IsNotEmpty",
        "IsInstanceOf", "IsNotInstanceOf",
        "IsAssignableFrom", "IsNotAssignableFrom",
        "Greater", "GreaterOrEqual", "Less", "LessOrEqual",
        "Contains", "DoesNotContain",
        "Throws", "ThrowsAsync", "DoesNotThrow", "DoesNotThrowAsync",
        "Pass", "Fail", "Inconclusive", "Ignore", "Warn",
        "Positive", "Negative", "Zero", "NotZero",
        "Catch", "CatchAsync"
    };

    private static readonly HashSet<string> NUnitAttributeNames = new()
    {
        "Test", "Theory", "TestCase", "TestCaseSource",
        "SetUp", "TearDown", "OneTimeSetUp", "OneTimeTearDown",
        "TestFixture", "Category", "Ignore", "Explicit",
        "Description", "Author", "Apartment",
        "Parallelizable", "NonParallelizable",
        "Repeat", "Values", "Range", "ValueSource",
        "Sequential", "Combinatorial", "Platform",
        "ExpectedException", "FixtureLifeCycle"
    };

    private static readonly HashSet<string> NUnitRemovableAttributeNames = new()
    {
        "TestFixture", // TestFixture is implicit in TUnit
        "Combinatorial", // TUnit's default behavior is combinatorial
        "Sequential", // No direct equivalent - TUnit uses Matrix which is combinatorial by default
        "Platform", // No direct equivalent - use custom SkipAttribute for platform-specific skipping
        "FixtureLifeCycle" // TUnit creates new instances by default (like InstancePerTestCase)
    };

    private static readonly HashSet<string> NUnitConditionallyRemovableAttributes = new()
    {
        "Parallelizable" // Only removed when NOT ParallelScope.None
    };

    public NUnitTwoPhaseAnalyzer(SemanticModel semanticModel, Compilation compilation)
        : base(semanticModel, compilation)
    {
    }

    protected override IEnumerable<InvocationExpressionSyntax> FindAssertionNodes(CompilationUnitSyntax root)
    {
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsNUnitAssertion);
    }

    private bool IsNUnitAssertion(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var typeName = GetSimpleTypeName(memberAccess.Expression);
            var methodName = memberAccess.Name.Identifier.Text;

            // Check classic Assert methods
            if (typeName is "Assert" or "ClassicAssert")
            {
                // Handle Assert.That (constraint-based) separately
                if (methodName == "That")
                    return true;

                if (NUnitClassicAssertMethods.Contains(methodName))
                    return VerifyNUnitNamespace(invocation);
            }

            // Check StringAssert, CollectionAssert, FileAssert, DirectoryAssert
            if (typeName is "StringAssert" or "CollectionAssert" or "FileAssert" or "DirectoryAssert")
                return VerifyNUnitNamespace(invocation);
        }

        return false;
    }

    private static string GetSimpleTypeName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => expression.ToString()
        };
    }

    private bool VerifyNUnitNamespace(InvocationExpressionSyntax invocation)
    {
        try
        {
            var symbolInfo = SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                var containingNamespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
                return containingNamespace?.StartsWith("NUnit.Framework") == true;
            }
        }
        catch
        {
            // Fall back to syntax-based detection
        }

        return true; // Assume it's NUnit if syntax matches
    }

    protected override AssertionConversion? AnalyzeAssertion(InvocationExpressionSyntax node)
    {
        if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        var typeName = GetSimpleTypeName(memberAccess.Expression);
        var methodName = memberAccess.Name.Identifier.Text;
        var arguments = node.ArgumentList.Arguments;

        // Handle constraint-based Assert.That
        if (typeName == "Assert" && methodName == "That")
        {
            return ConvertAssertThat(node, arguments, memberAccess);
        }

        // Handle classic assertions (Assert.* or ClassicAssert.*)
        if (typeName is "Assert" or "ClassicAssert")
        {
            var (kind, replacementCode, introducesAwait, todoComment) = ConvertClassicAssert(methodName, arguments, memberAccess);
            if (replacementCode == null)
                return null;

            return new AssertionConversion
            {
                Kind = kind,
                OriginalText = node.ToString(),
                ReplacementCode = replacementCode,
                IntroducesAwait = introducesAwait,
                TodoComment = todoComment
            };
        }

        // Handle StringAssert
        if (typeName == "StringAssert")
        {
            var (kind, replacementCode, introducesAwait, todoComment) = ConvertStringAssert(methodName, arguments);
            if (replacementCode == null)
                return null;

            return new AssertionConversion
            {
                Kind = kind,
                OriginalText = node.ToString(),
                ReplacementCode = replacementCode,
                IntroducesAwait = introducesAwait,
                TodoComment = todoComment
            };
        }

        // Handle CollectionAssert
        if (typeName == "CollectionAssert")
        {
            var (kind, replacementCode, introducesAwait, todoComment) = ConvertCollectionAssert(methodName, arguments);
            if (replacementCode == null)
                return null;

            return new AssertionConversion
            {
                Kind = kind,
                OriginalText = node.ToString(),
                ReplacementCode = replacementCode,
                IntroducesAwait = introducesAwait,
                TodoComment = todoComment
            };
        }

        // Handle FileAssert
        if (typeName == "FileAssert")
        {
            var (kind, replacementCode, introducesAwait, todoComment) = ConvertFileAssert(methodName, arguments);
            if (replacementCode == null)
                return null;

            return new AssertionConversion
            {
                Kind = kind,
                OriginalText = node.ToString(),
                ReplacementCode = replacementCode,
                IntroducesAwait = introducesAwait,
                TodoComment = todoComment
            };
        }

        // Handle DirectoryAssert
        if (typeName == "DirectoryAssert")
        {
            var (kind, replacementCode, introducesAwait, todoComment) = ConvertDirectoryAssert(methodName, arguments);
            if (replacementCode == null)
                return null;

            return new AssertionConversion
            {
                Kind = kind,
                OriginalText = node.ToString(),
                ReplacementCode = replacementCode,
                IntroducesAwait = introducesAwait,
                TodoComment = todoComment
            };
        }

        return null;
    }

    private AssertionConversion? ConvertAssertThat(
        InvocationExpressionSyntax node,
        SeparatedSyntaxList<ArgumentSyntax> args,
        MemberAccessExpressionSyntax memberAccess)
    {
        if (args.Count < 2)
            return null;

        var actualValue = args[0].Expression.ToString();
        var constraintArg = args[1].Expression;

        // Handle common constraint patterns
        var (kind, assertionSuffix, todoComment) = AnalyzeConstraint(constraintArg);

        if (assertionSuffix == null)
        {
            // Complex constraint - leave a TODO
            return new AssertionConversion
            {
                Kind = AssertionConversionKind.Unknown,
                OriginalText = node.ToString(),
                ReplacementCode = node.ToString(),
                IntroducesAwait = false,
                TodoComment = "// TODO: TUnit migration - Complex NUnit constraint. Manual conversion required."
            };
        }

        // Check for message parameter
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actualValue}){assertionSuffix}.Because({message})"
            : $"await Assert.That({actualValue}){assertionSuffix}";

        return new AssertionConversion
        {
            Kind = kind,
            OriginalText = node.ToString(),
            ReplacementCode = assertion,
            IntroducesAwait = true,
            TodoComment = todoComment
        };
    }

    private (AssertionConversionKind, string?, string?) AnalyzeConstraint(ExpressionSyntax constraint)
    {
        // Handle Is.EqualTo(expected)
        if (constraint is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var receiverText = memberAccess.Expression.ToString();
                var methodName = memberAccess.Name.Identifier.Text;
                var args = invocation.ArgumentList.Arguments;

                // Handle chained constraint modifiers like .Within(delta) on Is.EqualTo(x).Within(delta)
                if (methodName == "Within" && memberAccess.Expression is InvocationExpressionSyntax innerConstraint)
                {
                    // Get the base assertion (e.g., IsEqualTo(5)) first
                    var (kind, baseAssertion, todoComment) = AnalyzeConstraint(innerConstraint);
                    if (baseAssertion != null && args.Count >= 1)
                    {
                        var delta = args[0].Expression.ToString();
                        return (kind, $"{baseAssertion}.Within({delta})", todoComment);
                    }
                }

                // Is.EqualTo(expected)
                if (receiverText == "Is" && methodName == "EqualTo" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.Equal, $".IsEqualTo({expected})", null);
                }

                // Is.Not.EqualTo(expected)
                if (receiverText == "Is.Not" && methodName == "EqualTo" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.NotEqual, $".IsNotEqualTo({expected})", null);
                }

                // Is.SameAs(expected)
                if (receiverText == "Is" && methodName == "SameAs" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.Same, $".IsSameReferenceAs({expected})", null);
                }

                // Is.Not.SameAs(expected)
                if (receiverText == "Is.Not" && methodName == "SameAs" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.NotSame, $".IsNotSameReferenceAs({expected})", null);
                }

                // Is.InstanceOf<T>() or Is.InstanceOf(type)
                // InstanceOf checks if the value is an instance of T or a derived type, maps to IsAssignableTo
                if (receiverText == "Is" && methodName == "InstanceOf")
                {
                    if (memberAccess.Name is GenericNameSyntax genericName && genericName.TypeArgumentList.Arguments.Count > 0)
                    {
                        var typeArg = genericName.TypeArgumentList.Arguments[0].ToString();
                        return (AssertionConversionKind.IsAssignableFrom, $".IsAssignableTo<{typeArg}>()", null);
                    }
                    if (args.Count >= 1)
                    {
                        var typeArg = args[0].Expression.ToString();
                        return (AssertionConversionKind.IsAssignableFrom, $".IsAssignableTo({typeArg})", null);
                    }
                }

                // Is.TypeOf<T>()
                if (receiverText == "Is" && methodName == "TypeOf")
                {
                    if (memberAccess.Name is GenericNameSyntax genericName && genericName.TypeArgumentList.Arguments.Count > 0)
                    {
                        var typeArg = genericName.TypeArgumentList.Arguments[0].ToString();
                        return (AssertionConversionKind.IsType, $".IsTypeOf<{typeArg}>()", null);
                    }
                }

                // Is.AssignableTo<T>()
                if (receiverText == "Is" && methodName == "AssignableTo")
                {
                    if (memberAccess.Name is GenericNameSyntax genericName && genericName.TypeArgumentList.Arguments.Count > 0)
                    {
                        var typeArg = genericName.TypeArgumentList.Arguments[0].ToString();
                        return (AssertionConversionKind.IsAssignableFrom, $".IsAssignableTo<{typeArg}>()", null);
                    }
                }

                // Is.Not.InstanceOf<T>()
                if (receiverText == "Is.Not" && methodName == "InstanceOf")
                {
                    if (memberAccess.Name is GenericNameSyntax genericName && genericName.TypeArgumentList.Arguments.Count > 0)
                    {
                        var typeArg = genericName.TypeArgumentList.Arguments[0].ToString();
                        return (AssertionConversionKind.IsNotType, $".IsNotAssignableTo<{typeArg}>()", null);
                    }
                }

                // Is.Not.TypeOf<T>()
                if (receiverText == "Is.Not" && methodName == "TypeOf")
                {
                    if (memberAccess.Name is GenericNameSyntax genericName && genericName.TypeArgumentList.Arguments.Count > 0)
                    {
                        var typeArg = genericName.TypeArgumentList.Arguments[0].ToString();
                        return (AssertionConversionKind.IsNotType, $".IsNotTypeOf<{typeArg}>()", null);
                    }
                }

                // Does.StartWith(string)
                if (receiverText == "Does" && methodName == "StartWith" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.StartsWith, $".StartsWith({expected})", null);
                }

                // Does.EndWith(string)
                if (receiverText == "Does" && methodName == "EndWith" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.EndsWith, $".EndsWith({expected})", null);
                }

                // Does.Contain(string)
                if (receiverText == "Does" && methodName == "Contain" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.Contains, $".Contains({expected})", null);
                }

                // Does.Not.StartWith(string)
                if (receiverText == "Does.Not" && methodName == "StartWith" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.StartsWith, $".DoesNotStartWith({expected})", null);
                }

                // Does.Not.EndWith(string)
                if (receiverText == "Does.Not" && methodName == "EndWith" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.EndsWith, $".DoesNotEndWith({expected})", null);
                }

                // Does.Not.Contain(string) - for strings
                if (receiverText == "Does.Not" && methodName == "Contain" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.DoesNotContain, $".DoesNotContain({expected})", null);
                }

                // Does.Match(pattern)
                if (receiverText == "Does" && methodName == "Match" && args.Count >= 1)
                {
                    var pattern = args[0].Expression.ToString();
                    return (AssertionConversionKind.Matches, $".Matches({pattern})", null);
                }

                // Does.Not.Match(pattern)
                if (receiverText == "Does.Not" && methodName == "Match" && args.Count >= 1)
                {
                    var pattern = args[0].Expression.ToString();
                    return (AssertionConversionKind.Matches, $".DoesNotMatch({pattern})", null);
                }

                // Has.Count.EqualTo(n)
                if (receiverText == "Has.Count" && methodName == "EqualTo" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.Equal, $".Count().IsEqualTo({expected})", null);
                }

                // Has.Member(item)
                if (receiverText == "Has" && methodName == "Member" && args.Count >= 1)
                {
                    var item = args[0].Expression.ToString();
                    return (AssertionConversionKind.Contains, $".Contains({item})", null);
                }

                // Has.Exactly(n).Items
                if (memberAccess.Expression is InvocationExpressionSyntax hasExactlyInvocation &&
                    hasExactlyInvocation.Expression is MemberAccessExpressionSyntax hasExactlyAccess &&
                    hasExactlyAccess.Expression.ToString() == "Has" &&
                    hasExactlyAccess.Name.Identifier.Text == "Exactly" &&
                    methodName == "Items" &&
                    hasExactlyInvocation.ArgumentList.Arguments.Count >= 1)
                {
                    var count = hasExactlyInvocation.ArgumentList.Arguments[0].Expression.ToString();
                    return (AssertionConversionKind.Collection, $".Count().IsEqualTo({count})", null);
                }

                // Is.GreaterThan(expected)
                if (receiverText == "Is" && methodName == "GreaterThan" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.InRange, $".IsGreaterThan({expected})", null);
                }

                // Is.LessThan(expected)
                if (receiverText == "Is" && methodName == "LessThan" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.InRange, $".IsLessThan({expected})", null);
                }

                // Is.GreaterThanOrEqualTo(expected)
                if (receiverText == "Is" && methodName == "GreaterThanOrEqualTo" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.InRange, $".IsGreaterThanOrEqualTo({expected})", null);
                }

                // Is.LessThanOrEqualTo(expected)
                if (receiverText == "Is" && methodName == "LessThanOrEqualTo" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.InRange, $".IsLessThanOrEqualTo({expected})", null);
                }

                // Is.Not.GreaterThan(expected) → IsLessThanOrEqualTo
                if (receiverText == "Is.Not" && methodName == "GreaterThan" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.InRange, $".IsLessThanOrEqualTo({expected})", null);
                }

                // Is.Not.LessThan(expected) → IsGreaterThanOrEqualTo
                if (receiverText == "Is.Not" && methodName == "LessThan" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.InRange, $".IsGreaterThanOrEqualTo({expected})", null);
                }

                // Is.Not.GreaterThanOrEqualTo(expected) → IsLessThan
                if (receiverText == "Is.Not" && methodName == "GreaterThanOrEqualTo" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.InRange, $".IsLessThan({expected})", null);
                }

                // Is.Not.LessThanOrEqualTo(expected) → IsGreaterThan
                if (receiverText == "Is.Not" && methodName == "LessThanOrEqualTo" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.InRange, $".IsGreaterThan({expected})", null);
                }

                // Contains.Item(expected)
                if ((receiverText == "Contains" || receiverText == "Does.Contain") && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.Contains, $".Contains({expected})", null);
                }

                // Does.Not.Contain / Does.Not.Contain(expected)
                if (receiverText == "Does.Not" && methodName == "Contain" && args.Count >= 1)
                {
                    var expected = args[0].Expression.ToString();
                    return (AssertionConversionKind.DoesNotContain, $".DoesNotContain({expected})", null);
                }

                // Throws.TypeOf<T>()
                if (receiverText == "Throws" && methodName == "TypeOf")
                {
                    if (memberAccess.Name is GenericNameSyntax genericThrows && genericThrows.TypeArgumentList.Arguments.Count > 0)
                    {
                        var exceptionType = genericThrows.TypeArgumentList.Arguments[0].ToString();
                        return (AssertionConversionKind.Throws, null, $"// TODO: TUnit migration - Convert to Assert.ThrowsAsync<{exceptionType}>()");
                    }
                }
            }
        }

        // Handle simple property constraints
        if (constraint is MemberAccessExpressionSyntax simpleMemberAccess)
        {
            var fullConstraint = simpleMemberAccess.ToString();

            // Is.True
            if (fullConstraint is "Is.True")
                return (AssertionConversionKind.True, ".IsTrue()", null);

            // Is.False
            if (fullConstraint is "Is.False")
                return (AssertionConversionKind.False, ".IsFalse()", null);

            // Is.Null
            if (fullConstraint is "Is.Null")
                return (AssertionConversionKind.Null, ".IsNull()", null);

            // Is.Not.Null
            if (fullConstraint is "Is.Not.Null")
                return (AssertionConversionKind.NotNull, ".IsNotNull()", null);

            // Is.Empty
            if (fullConstraint is "Is.Empty")
                return (AssertionConversionKind.Empty, ".IsEmpty()", null);

            // Is.Not.Empty
            if (fullConstraint is "Is.Not.Empty")
                return (AssertionConversionKind.NotEmpty, ".IsNotEmpty()", null);

            // Is.Positive
            if (fullConstraint is "Is.Positive")
                return (AssertionConversionKind.InRange, ".IsPositive()", null);

            // Is.Negative
            if (fullConstraint is "Is.Negative")
                return (AssertionConversionKind.InRange, ".IsNegative()", null);

            // Is.Zero
            if (fullConstraint is "Is.Zero")
                return (AssertionConversionKind.Equal, ".IsZero()", null);

            // Is.Not.Zero
            if (fullConstraint is "Is.Not.Zero")
                return (AssertionConversionKind.NotEqual, ".IsNotZero()", null);

            // Is.Not.Positive - means value <= 0
            if (fullConstraint is "Is.Not.Positive")
                return (AssertionConversionKind.InRange, ".IsLessThanOrEqualTo(0)", null);

            // Is.Not.Negative - means value >= 0
            if (fullConstraint is "Is.Not.Negative")
                return (AssertionConversionKind.InRange, ".IsGreaterThanOrEqualTo(0)", null);

            // Is.NaN
            if (fullConstraint is "Is.NaN")
                return (AssertionConversionKind.Equal, ".IsNaN()", null);

            // Is.Not.NaN
            if (fullConstraint is "Is.Not.NaN")
                return (AssertionConversionKind.NotEqual, ".IsNotNaN()", null);

            // Is.Ordered (default ascending) - use generic .IsInOrder()
            if (fullConstraint is "Is.Ordered")
                return (AssertionConversionKind.Collection, ".IsInOrder()", null);

            // Is.Ordered.Ascending - use generic .IsInOrder()
            if (fullConstraint is "Is.Ordered.Ascending")
                return (AssertionConversionKind.Collection, ".IsInOrder()", null);

            // Is.Ordered.Descending
            if (fullConstraint is "Is.Ordered.Descending")
                return (AssertionConversionKind.Collection, ".IsInDescendingOrder()", null);

            // Is.Unique
            if (fullConstraint is "Is.Unique")
                return (AssertionConversionKind.Collection, ".HasDistinctItems()", null);

            // Has.Exactly(n).Items - property access pattern
            if (simpleMemberAccess.Name.Identifier.Text == "Items" &&
                simpleMemberAccess.Expression is InvocationExpressionSyntax hasExactlyInvocation &&
                hasExactlyInvocation.Expression is MemberAccessExpressionSyntax hasExactlyAccess &&
                hasExactlyAccess.Expression.ToString() == "Has" &&
                hasExactlyAccess.Name.Identifier.Text == "Exactly" &&
                hasExactlyInvocation.ArgumentList.Arguments.Count >= 1)
            {
                var count = hasExactlyInvocation.ArgumentList.Arguments[0].Expression.ToString();
                return (AssertionConversionKind.Collection, $".Count().IsEqualTo({count})", null);
            }
        }

        // Unknown constraint
        return (AssertionConversionKind.Unknown, null, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertClassicAssert(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args, MemberAccessExpressionSyntax memberAccess)
    {
        return methodName switch
        {
            "AreEqual" => ConvertAreEqual(args),
            "AreNotEqual" => ConvertAreNotEqual(args),
            "AreSame" => ConvertAreSame(args),
            "AreNotSame" => ConvertAreNotSame(args),
            "IsTrue" => ConvertIsTrue(args),
            "IsFalse" => ConvertIsFalse(args),
            "IsNull" => ConvertIsNull(args),
            "IsNotNull" => ConvertIsNotNull(args),
            "IsEmpty" => ConvertIsEmpty(args),
            "IsNotEmpty" => ConvertIsNotEmpty(args),
            "Greater" => ConvertGreater(args),
            "GreaterOrEqual" => ConvertGreaterOrEqual(args),
            "Less" => ConvertLess(args),
            "LessOrEqual" => ConvertLessOrEqual(args),
            "Contains" => ConvertContains(args),
            "Pass" => (AssertionConversionKind.Skip, "// Test passed", false, null),
            "Fail" when args.Count > 0 => (AssertionConversionKind.Fail, $"Fail.Test({args[0].Expression})", false, null),
            "Fail" => (AssertionConversionKind.Fail, "Fail.Test(\"\")", false, null),
            "Inconclusive" when args.Count > 0 => (AssertionConversionKind.Inconclusive, $"Skip.Test({args[0].Expression})", false, null),
            "Inconclusive" => (AssertionConversionKind.Inconclusive, "Skip.Test(\"Test inconclusive\")", false, null),
            "Ignore" when args.Count > 0 => (AssertionConversionKind.Skip, $"Skip.Test({args[0].Expression})", false, null),
            "Ignore" => (AssertionConversionKind.Skip, "Skip.Test(\"Ignored\")", false, null),
            "Throws" or "ThrowsAsync" => ConvertThrows(memberAccess, args),
            "Catch" or "CatchAsync" => ConvertCatch(memberAccess, args),
            "DoesNotThrow" or "DoesNotThrowAsync" => ConvertDoesNotThrow(args),
            "Positive" when args.Count >= 1 => (AssertionConversionKind.InRange, $"await Assert.That({args[0].Expression}).IsPositive()", true, null),
            "Negative" when args.Count >= 1 => (AssertionConversionKind.InRange, $"await Assert.That({args[0].Expression}).IsNegative()", true, null),
            "Zero" when args.Count >= 1 => (AssertionConversionKind.Equal, $"await Assert.That({args[0].Expression}).IsZero()", true, null),
            "NotZero" when args.Count >= 1 => (AssertionConversionKind.NotEqual, $"await Assert.That({args[0].Expression}).IsNotZero()", true, null),
            "Warn" when args.Count > 0 => (AssertionConversionKind.Skip, $"Skip.Test($\"Warning: {{{args[0].Expression}}}\")", false, null),
            "Warn" => (AssertionConversionKind.Skip, "Skip.Test(\"Warning\")", false, null),
            _ => (AssertionConversionKind.Unknown, null, false, null)
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertAreEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.AreEqual, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsEqualTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsEqualTo({expected})";

        return (AssertionConversionKind.AreEqual, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertAreNotEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.AreNotEqual, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsNotEqualTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsNotEqualTo({expected})";

        return (AssertionConversionKind.AreNotEqual, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertAreSame(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.AreSame, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsSameReferenceAs({expected}).Because({message})"
            : $"await Assert.That({actual}).IsSameReferenceAs({expected})";

        return (AssertionConversionKind.AreSame, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertAreNotSame(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.AreNotSame, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsNotSameReferenceAs({expected}).Because({message})"
            : $"await Assert.That({actual}).IsNotSameReferenceAs({expected})";

        return (AssertionConversionKind.AreNotSame, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsTrue(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsTrue, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsTrue().Because({message})"
            : $"await Assert.That({value}).IsTrue()";

        return (AssertionConversionKind.IsTrue, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsFalse(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsFalse, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsFalse().Because({message})"
            : $"await Assert.That({value}).IsFalse()";

        return (AssertionConversionKind.IsFalse, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsNull(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsNull, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsNull().Because({message})"
            : $"await Assert.That({value}).IsNull()";

        return (AssertionConversionKind.IsNull, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsNotNull(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsNotNull, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsNotNull().Because({message})"
            : $"await Assert.That({value}).IsNotNull()";

        return (AssertionConversionKind.IsNotNull, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsEmpty(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.Empty, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsEmpty().Because({message})"
            : $"await Assert.That({value}).IsEmpty()";

        return (AssertionConversionKind.Empty, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsNotEmpty(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.NotEmpty, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsNotEmpty().Because({message})"
            : $"await Assert.That({value}).IsNotEmpty()";

        return (AssertionConversionKind.NotEmpty, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertGreater(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.InRange, null, false, null);

        var actual = args[0].Expression.ToString();
        var expected = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsGreaterThan({expected}).Because({message})"
            : $"await Assert.That({actual}).IsGreaterThan({expected})";

        return (AssertionConversionKind.InRange, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertGreaterOrEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.InRange, null, false, null);

        var actual = args[0].Expression.ToString();
        var expected = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsGreaterThanOrEqualTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsGreaterThanOrEqualTo({expected})";

        return (AssertionConversionKind.InRange, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertLess(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.InRange, null, false, null);

        var actual = args[0].Expression.ToString();
        var expected = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsLessThan({expected}).Because({message})"
            : $"await Assert.That({actual}).IsLessThan({expected})";

        return (AssertionConversionKind.InRange, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertLessOrEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.InRange, null, false, null);

        var actual = args[0].Expression.ToString();
        var expected = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsLessThanOrEqualTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsLessThanOrEqualTo({expected})";

        return (AssertionConversionKind.InRange, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertContains(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Contains, null, false, null);

        var expected = args[0].Expression.ToString();
        var collection = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).Contains({expected}).Because({message})"
            : $"await Assert.That({collection}).Contains({expected})";

        return (AssertionConversionKind.Contains, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertThrows(
        MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.Throws, null, false, null);

        string typeArg = "Exception";
        string action;

        // Check if generic: Assert.Throws<T>(action)
        if (memberAccess.Name is GenericNameSyntax genericName &&
            genericName.TypeArgumentList.Arguments.Count > 0)
        {
            typeArg = genericName.TypeArgumentList.Arguments[0].ToString();
            action = args[0].Expression.ToString();
        }
        // Check if constraint form: Assert.Throws(Is.TypeOf(typeof(T)), action)
        else if (args.Count >= 2)
        {
            var constraintExpr = args[0].Expression;
            action = args[1].Expression.ToString();

            // Try to extract type from constraint
            typeArg = TryExtractTypeFromThrowsConstraint(constraintExpr) ?? "Exception";
        }
        else
        {
            action = args[0].Expression.ToString();
        }

        var assertion = $"await Assert.ThrowsAsync<{typeArg}>({action})";
        return (AssertionConversionKind.Throws, assertion, true, null);
    }

    private static string? TryExtractTypeFromThrowsConstraint(ExpressionSyntax constraint)
    {
        // Handle Is.TypeOf(typeof(ArgumentException))
        if (constraint is InvocationExpressionSyntax invocation)
        {
            var invocationText = invocation.ToString();
            if (invocationText.StartsWith("Is.TypeOf(typeof("))
            {
                // Extract type from Is.TypeOf(typeof(ArgumentException))
                var start = "Is.TypeOf(typeof(".Length;
                var end = invocationText.LastIndexOf("))");
                if (end > start)
                {
                    return invocationText.Substring(start, end - start);
                }
            }
            // Handle Is.TypeOf<T>()
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is GenericNameSyntax genericName &&
                genericName.Identifier.Text == "TypeOf" &&
                genericName.TypeArgumentList.Arguments.Count > 0)
            {
                return genericName.TypeArgumentList.Arguments[0].ToString();
            }
        }
        // Handle Is.InstanceOf<T>()
        if (constraint is InvocationExpressionSyntax instanceOfInvocation &&
            instanceOfInvocation.Expression is MemberAccessExpressionSyntax instanceOfAccess &&
            instanceOfAccess.Name is GenericNameSyntax instanceOfGeneric &&
            instanceOfGeneric.Identifier.Text == "InstanceOf" &&
            instanceOfGeneric.TypeArgumentList.Arguments.Count > 0)
        {
            return instanceOfGeneric.TypeArgumentList.Arguments[0].ToString();
        }
        return null;
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCatch(
        MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        // Catch is similar to Throws but returns the exception
        return ConvertThrows(memberAccess, args);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertDoesNotThrow(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.Throws, null, false, null);

        var action = args[0].Expression.ToString();

        // DoesNotThrow is simply invoking the action - if it throws, the test fails
        var assertion = $"await Assert.That({action}).ThrowsNothing()";
        return (AssertionConversionKind.Throws, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringAssert(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        return methodName switch
        {
            "Contains" when args.Count >= 2 => ConvertStringContains(args),
            "StartsWith" when args.Count >= 2 => ConvertStringStartsWith(args),
            "EndsWith" when args.Count >= 2 => ConvertStringEndsWith(args),
            "AreEqualIgnoringCase" when args.Count >= 2 => ConvertStringAreEqualIgnoringCase(args),
            "IsMatch" when args.Count >= 2 => ConvertStringMatches(args),
            "DoesNotMatch" when args.Count >= 2 => ConvertStringDoesNotMatch(args),
            _ => (AssertionConversionKind.Unknown, null, false, null)
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringContains(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).Contains({expected}).Because({message})"
            : $"await Assert.That({actual}).Contains({expected})";

        return (AssertionConversionKind.StringContains, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringStartsWith(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).StartsWith({expected}).Because({message})"
            : $"await Assert.That({actual}).StartsWith({expected})";

        return (AssertionConversionKind.StringStartsWith, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringEndsWith(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).EndsWith({expected}).Because({message})"
            : $"await Assert.That({actual}).EndsWith({expected})";

        return (AssertionConversionKind.StringEndsWith, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringAreEqualIgnoringCase(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsEqualTo({expected}, StringComparison.OrdinalIgnoreCase).Because({message})"
            : $"await Assert.That({actual}).IsEqualTo({expected}, StringComparison.OrdinalIgnoreCase)";

        return (AssertionConversionKind.AreEqual, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringMatches(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var pattern = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).Matches({pattern}).Because({message})"
            : $"await Assert.That({actual}).Matches({pattern})";

        return (AssertionConversionKind.StringMatches, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringDoesNotMatch(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var pattern = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).DoesNotMatch({pattern}).Because({message})"
            : $"await Assert.That({actual}).DoesNotMatch({pattern})";

        return (AssertionConversionKind.StringDoesNotMatch, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAssert(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        return methodName switch
        {
            "AreEqual" when args.Count >= 2 => ConvertCollectionAreEqual(args),
            "AreNotEqual" when args.Count >= 2 => ConvertCollectionAreNotEqual(args),
            "AreEquivalent" when args.Count >= 2 => ConvertCollectionAreEquivalent(args),
            "AreNotEquivalent" when args.Count >= 2 => ConvertCollectionAreNotEquivalent(args),
            "Contains" when args.Count >= 2 => ConvertCollectionContains(args),
            "DoesNotContain" when args.Count >= 2 => ConvertCollectionDoesNotContain(args),
            "IsSubsetOf" when args.Count >= 2 => ConvertCollectionIsSubsetOf(args),
            "IsNotSubsetOf" when args.Count >= 2 => ConvertCollectionIsNotSubsetOf(args),
            "AllItemsAreUnique" when args.Count >= 1 => ConvertCollectionAllItemsAreUnique(args),
            "AllItemsAreNotNull" when args.Count >= 1 => ConvertCollectionAllItemsAreNotNull(args),
            "IsEmpty" when args.Count >= 1 => ConvertCollectionIsEmpty(args),
            "IsNotEmpty" when args.Count >= 1 => ConvertCollectionIsNotEmpty(args),
            _ => (AssertionConversionKind.Unknown, null, false, null)
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAreEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsEquivalentTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsEquivalentTo({expected})";

        return (AssertionConversionKind.CollectionAreEqual, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAreNotEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsNotEquivalentTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsNotEquivalentTo({expected})";

        return (AssertionConversionKind.CollectionAreNotEqual, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAreEquivalent(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsEquivalentTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsEquivalentTo({expected})";

        return (AssertionConversionKind.CollectionAreEquivalent, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAreNotEquivalent(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsNotEquivalentTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsNotEquivalentTo({expected})";

        return (AssertionConversionKind.CollectionAreNotEquivalent, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionContains(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var collection = args[0].Expression.ToString();
        var element = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).Contains({element}).Because({message})"
            : $"await Assert.That({collection}).Contains({element})";

        return (AssertionConversionKind.CollectionContains, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionDoesNotContain(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var collection = args[0].Expression.ToString();
        var element = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).DoesNotContain({element}).Because({message})"
            : $"await Assert.That({collection}).DoesNotContain({element})";

        return (AssertionConversionKind.CollectionDoesNotContain, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionIsSubsetOf(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var subset = args[0].Expression.ToString();
        var superset = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({subset}).IsSubsetOf({superset}).Because({message})"
            : $"await Assert.That({subset}).IsSubsetOf({superset})";

        return (AssertionConversionKind.CollectionIsSubsetOf, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionIsNotSubsetOf(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var subset = args[0].Expression.ToString();
        var superset = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That({subset}).IsNotSubsetOf({superset}).Because({message})"
            : $"await Assert.That({subset}).IsNotSubsetOf({superset})";

        return (AssertionConversionKind.CollectionIsNotSubsetOf, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAllItemsAreUnique(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var collection = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).HasDistinctItems().Because({message})"
            : $"await Assert.That({collection}).HasDistinctItems()";

        return (AssertionConversionKind.CollectionAllItemsAreUnique, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAllItemsAreNotNull(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var collection = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).All(x => x != null).Because({message})"
            : $"await Assert.That({collection}).All(x => x != null)";

        return (AssertionConversionKind.CollectionAllItemsAreNotNull, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionIsEmpty(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var collection = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).IsEmpty().Because({message})"
            : $"await Assert.That({collection}).IsEmpty()";

        return (AssertionConversionKind.Empty, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionIsNotEmpty(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var collection = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args[1]) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).IsNotEmpty().Because({message})"
            : $"await Assert.That({collection}).IsNotEmpty()";

        return (AssertionConversionKind.NotEmpty, assertion, true, null);
    }

    private static string? GetMessageArgument(ArgumentSyntax arg)
    {
        // NUnit message parameters can be string literals or expressions
        if (arg.Expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return arg.Expression.ToString();
        }
        if (arg.Expression is InterpolatedStringExpressionSyntax)
        {
            return arg.Expression.ToString();
        }
        // Return the expression as-is for other cases
        return arg.Expression.ToString();
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertFileAssert(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1)
            return (AssertionConversionKind.Unknown, null, false, null);

        var path = args[0].Expression.ToString();

        return methodName switch
        {
            "Exists" => (AssertionConversionKind.True, $"await Assert.That(File.Exists({path})).IsTrue()", true, null),
            "DoesNotExist" => (AssertionConversionKind.True, $"await Assert.That(File.Exists({path})).IsFalse()", true, null),
            "AreEqual" when args.Count >= 2 => ConvertFileAreEqual(args),
            "AreNotEqual" when args.Count >= 2 => ConvertFileAreNotEqual(args),
            _ => (AssertionConversionKind.Unknown, null, false, $"// TODO: TUnit migration - FileAssert.{methodName} has no direct equivalent")
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertFileAreEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That(new FileInfo({actual})).HasSameContentAs(new FileInfo({expected})).Because({message})"
            : $"await Assert.That(new FileInfo({actual})).HasSameContentAs(new FileInfo({expected}))";

        return (AssertionConversionKind.Equal, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertFileAreNotEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That(new FileInfo({actual})).DoesNotHaveSameContentAs(new FileInfo({expected})).Because({message})"
            : $"await Assert.That(new FileInfo({actual})).DoesNotHaveSameContentAs(new FileInfo({expected}))";

        return (AssertionConversionKind.NotEqual, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertDirectoryAssert(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1)
            return (AssertionConversionKind.Unknown, null, false, null);

        var path = args[0].Expression.ToString();

        return methodName switch
        {
            "Exists" => (AssertionConversionKind.True, $"await Assert.That(Directory.Exists({path})).IsTrue()", true, null),
            "DoesNotExist" => (AssertionConversionKind.True, $"await Assert.That(Directory.Exists({path})).IsFalse()", true, null),
            "AreEqual" when args.Count >= 2 => ConvertDirectoryAreEqual(args),
            "AreNotEqual" when args.Count >= 2 => ConvertDirectoryAreNotEqual(args),
            _ => (AssertionConversionKind.Unknown, null, false, $"// TODO: TUnit migration - DirectoryAssert.{methodName} has no direct equivalent")
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertDirectoryAreEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That(new DirectoryInfo({actual})).IsEquivalentTo(new DirectoryInfo({expected})).Because({message})"
            : $"await Assert.That(new DirectoryInfo({actual})).IsEquivalentTo(new DirectoryInfo({expected}))";

        return (AssertionConversionKind.Equal, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertDirectoryAreNotEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args[2]) : null;

        var assertion = message != null
            ? $"await Assert.That(new DirectoryInfo({actual})).IsNotEquivalentTo(new DirectoryInfo({expected})).Because({message})"
            : $"await Assert.That(new DirectoryInfo({actual})).IsNotEquivalentTo(new DirectoryInfo({expected}))";

        return (AssertionConversionKind.NotEqual, assertion, true, null);
    }

    protected override bool ShouldRemoveAttribute(AttributeSyntax node)
    {
        var name = MigrationHelpers.GetAttributeName(node);

        if (NUnitRemovableAttributeNames.Contains(name))
            return true;

        // Handle conditionally removable attributes
        if (name == "Parallelizable")
        {
            // Remove Parallelizable unless it has ParallelScope.None (which converts to NotInParallel)
            if (node.ArgumentList?.Arguments.Count > 0)
            {
                var arg = node.ArgumentList.Arguments[0].Expression.ToString();
                if (arg.Contains("None"))
                    return false; // Don't remove - this will be converted to NotInParallel
            }
            return true; // Remove - TUnit is parallel by default
        }

        return false;
    }

    protected override AttributeConversion? AnalyzeAttribute(AttributeSyntax node)
    {
        var name = MigrationHelpers.GetAttributeName(node);

        if (!NUnitAttributeNames.Contains(name))
            return null;

        // Handle TestCase specially due to complex property conversions
        if (name == "TestCase")
        {
            return ConvertTestCaseAttribute(node);
        }

        // Handle Test specially if it has properties
        if (name == "Test")
        {
            return ConvertTestAttributeFull(node);
        }

        var (newName, newArgs) = name switch
        {
            "Theory" => ("Test", null),
            "TestCaseSource" => ("MethodDataSource", ConvertTestCaseSourceArgs(node)),
            "SetUp" => ("Before", "(HookType.Test)"),
            "TearDown" => ("After", "(HookType.Test)"),
            "OneTimeSetUp" => ("Before", "(HookType.Class)"),
            "OneTimeTearDown" => ("After", "(HookType.Class)"),
            "Category" => ("Category", node.ArgumentList?.ToString()),
            "Ignore" => ("Skip", node.ArgumentList?.ToString()),
            "Explicit" => ("Explicit", node.ArgumentList?.ToString()),
            "Description" => ("Property", ConvertDescriptionArgs(node)),
            "Author" => ("Property", ConvertAuthorArgs(node)),
            "Repeat" => ("Repeat", node.ArgumentList?.ToString()),
            "Values" => ("Matrix", node.ArgumentList?.ToString()),
            "ValueSource" => ("MatrixSourceMethod", node.ArgumentList?.ToString()),
            "NonParallelizable" => ("NotInParallel", null),
            "Parallelizable" => ConvertParallelizableAttribute(node),
            "Platform" => ConvertPlatformAttribute(node),
            "Apartment" => ConvertApartmentAttribute(node),
            "ExpectedException" => (null, null), // Handled separately
            "Sequential" => (null, null), // No direct equivalent - removed
            "FixtureLifeCycle" => (null, null), // TUnit uses instance-per-test by default - removed
            _ => (null, null)
        };

        if (newName == null)
            return null;

        return new AttributeConversion
        {
            NewAttributeName = newName,
            NewArgumentList = newArgs,
            OriginalText = node.ToString()
        };
    }

    private static string? ConvertTestCaseSourceArgs(AttributeSyntax node)
    {
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = node.ArgumentList.Arguments[0];
            return $"({firstArg.Expression})";
        }
        return null;
    }

    private static string? ConvertDescriptionArgs(AttributeSyntax node)
    {
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var value = node.ArgumentList.Arguments[0].Expression.ToString();
            return $"(\"Description\", {value})";
        }
        return null;
    }

    private static string? ConvertAuthorArgs(AttributeSyntax node)
    {
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var value = node.ArgumentList.Arguments[0].Expression.ToString();
            return $"(\"Author\", {value})";
        }
        return null;
    }

    private AttributeConversion ConvertTestAttributeFull(AttributeSyntax node)
    {
        // Handle [Test(Description = "...", Author = "...")]
        if (node.ArgumentList?.Arguments.Count == 0 || node.ArgumentList == null)
        {
            return new AttributeConversion
            {
                NewAttributeName = "Test",
                NewArgumentList = null,
                OriginalText = node.ToString()
            };
        }

        // Check for Description, Author properties
        var additionalAttributes = new List<AdditionalAttribute>();
        foreach (var arg in node.ArgumentList.Arguments)
        {
            if (arg.NameEquals != null)
            {
                var propName = arg.NameEquals.Name.Identifier.Text;
                var propValue = arg.Expression.ToString();

                switch (propName)
                {
                    case "Description":
                        additionalAttributes.Add(new AdditionalAttribute
                        {
                            Name = "Property",
                            Arguments = $"(\"Description\", {propValue})"
                        });
                        break;
                    case "Author":
                        additionalAttributes.Add(new AdditionalAttribute
                        {
                            Name = "Property",
                            Arguments = $"(\"Author\", {propValue})"
                        });
                        break;
                    case "ExpectedResult":
                        // ExpectedResult is handled separately by the ExpectedResult rewriter
                        break;
                }
            }
        }

        // Test attribute has no positional arguments in TUnit
        // Use empty string to explicitly remove arguments (null would keep them)
        return new AttributeConversion
        {
            NewAttributeName = "Test",
            NewArgumentList = "",
            AdditionalAttributes = additionalAttributes.Count > 0 ? additionalAttributes : null,
            OriginalText = node.ToString()
        };
    }

    private AttributeConversion? ConvertTestCaseAttribute(AttributeSyntax node)
    {
        // [TestCase(1, TestName = "Test")] -> [Arguments(1, DisplayName = "Test")]
        // [TestCase(1, Category = "Unit")] -> [Arguments(1, Categories = ["Unit"])]
        // [TestCase(1, Ignore = "reason")] -> [Arguments(1, Skip = "reason")]
        // [TestCase(1, IgnoreReason = "reason")] -> [Arguments(1, Skip = "reason")]
        // [TestCase(1, Description = "...")] -> [Arguments(1)] + [Property("Description", "...")]
        // [TestCase(1, Author = "...")] -> [Arguments(1)] + [Property("Author", "...")]
        // [TestCase(1, Explicit = true)] -> [Arguments(1)] + [Explicit]
        // [TestCase(1, ExplicitReason = "...")] -> [Arguments(1)] + [Explicit] + [Property("ExplicitReason", "...")]

        if (node.ArgumentList == null)
        {
            return new AttributeConversion
            {
                NewAttributeName = "Arguments",
                NewArgumentList = null,
                OriginalText = node.ToString()
            };
        }

        var positionalArgs = new List<string>();
        var inlineNamedArgs = new List<string>();
        var additionalAttributes = new List<AdditionalAttribute>();
        var hasExpectedResult = false;

        foreach (var arg in node.ArgumentList.Arguments)
        {
            if (arg.NameEquals != null || arg.NameColon != null)
            {
                // Named argument
                var propName = arg.NameEquals?.Name.Identifier.Text
                    ?? arg.NameColon?.Name.Identifier.Text
                    ?? "";
                var propValue = arg.Expression.ToString();

                switch (propName)
                {
                    case "TestName":
                        // Convert to DisplayName (inline)
                        inlineNamedArgs.Add($"DisplayName = {propValue}");
                        break;
                    case "Category":
                        // Convert to Categories array (inline)
                        inlineNamedArgs.Add($"Categories = [{propValue}]");
                        break;
                    case "Ignore":
                    case "IgnoreReason":
                        // Convert to Skip (inline)
                        inlineNamedArgs.Add($"Skip = {propValue}");
                        break;
                    case "Description":
                        // Convert to separate [Property] attribute
                        additionalAttributes.Add(new AdditionalAttribute
                        {
                            Name = "Property",
                            Arguments = $"(\"Description\", {propValue})"
                        });
                        break;
                    case "Author":
                        // Convert to separate [Property] attribute
                        additionalAttributes.Add(new AdditionalAttribute
                        {
                            Name = "Property",
                            Arguments = $"(\"Author\", {propValue})"
                        });
                        break;
                    case "Explicit":
                        // Convert to separate [Explicit] attribute (only if true)
                        if (propValue == "true")
                        {
                            additionalAttributes.Add(new AdditionalAttribute { Name = "Explicit" });
                        }
                        break;
                    case "ExplicitReason":
                        // Convert to [Explicit] + [Property]
                        additionalAttributes.Add(new AdditionalAttribute { Name = "Explicit" });
                        additionalAttributes.Add(new AdditionalAttribute
                        {
                            Name = "Property",
                            Arguments = $"(\"ExplicitReason\", {propValue})"
                        });
                        break;
                    case "ExpectedResult":
                        // ExpectedResult is handled separately by the ExpectedResult rewriter
                        hasExpectedResult = true;
                        break;
                }
            }
            else
            {
                // Positional argument - keep as-is
                positionalArgs.Add(arg.ToString());
            }
        }

        // If this TestCase has ExpectedResult, don't convert it here.
        // Let the NUnitExpectedResultRewriter handle the complete transformation.
        if (hasExpectedResult)
        {
            return null;
        }

        // Build the new argument list
        var allArgs = new List<string>(positionalArgs);
        allArgs.AddRange(inlineNamedArgs);
        var newArgList = allArgs.Count > 0 ? $"({string.Join(", ", allArgs)})" : null;

        return new AttributeConversion
        {
            NewAttributeName = "Arguments",
            NewArgumentList = newArgList,
            AdditionalAttributes = additionalAttributes.Count > 0 ? additionalAttributes : null,
            OriginalText = node.ToString()
        };
    }

    private (string?, string?) ConvertParallelizableAttribute(AttributeSyntax node)
    {
        // [Parallelizable(ParallelScope.None)] -> [NotInParallel]
        // [Parallelizable(ParallelScope.Self)] -> Keep as default (TUnit default is parallel)
        // [Parallelizable] with no args -> Keep as default
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var arg = node.ArgumentList.Arguments[0].Expression.ToString();
            if (arg.Contains("None"))
            {
                return ("NotInParallel", ""); // Empty string = no arguments
            }
        }
        // Default parallelizable - TUnit is parallel by default, so we can remove this
        return (null, null);
    }

    private (string?, string?) ConvertPlatformAttribute(AttributeSyntax node)
    {
        // [Platform] attribute has no direct equivalent in TUnit.
        // - TUnit doesn't have [ExcludeOn] for platform exclusion
        // - [RunOn] exists but platform mapping is imprecise
        // The attribute is in NUnitRemovableAttributeNames and will be removed.
        // Users should implement custom SkipAttribute for platform-specific skipping.
        return (null, null);
    }

    private (string?, string?) ConvertApartmentAttribute(AttributeSyntax node)
    {
        // [Apartment(ApartmentState.STA)] -> [STAThreadExecutor]
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var arg = node.ArgumentList.Arguments[0].Expression.ToString();
            if (arg.Contains("STA"))
            {
                return ("STAThreadExecutor", "");  // Empty string to remove arguments
            }
        }
        return (null, null);
    }

    protected override ParameterAttributeConversion? AnalyzeParameterAttribute(AttributeSyntax attr, ParameterSyntax parameter)
    {
        var attrName = MigrationHelpers.GetAttributeName(attr);

        if (attrName == "Range")
        {
            return ConvertRangeAttribute(attr, parameter);
        }

        return null;
    }

    protected override CompilationUnitSyntax AnalyzeMethodsForMissingAttributes(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Find methods that have TestCase/Arguments attributes but no Test attribute
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            try
            {
                var hasTestCaseOrArguments = false;
                var hasTestAttribute = false;

                foreach (var attrList in method.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrName = MigrationHelpers.GetAttributeName(attr);
                        if (attrName == "TestCase" || attrName == "Arguments" || attrName == "TestCaseSource")
                        {
                            hasTestCaseOrArguments = true;
                        }
                        else if (attrName == "Test" || attrName == "Theory")
                        {
                            hasTestAttribute = true;
                        }
                    }
                }

                if (hasTestCaseOrArguments && !hasTestAttribute)
                {
                    // Need to add [Test] attribute
                    var addition = new MethodAttributeAddition
                    {
                        AttributeCode = "Test",
                        OriginalText = method.Identifier.Text
                    };
                    Plan.MethodAttributeAdditions.Add(addition);

                    // Annotate the method so we can find it during transformation
                    var nodeToAnnotate = currentRoot.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(m => m.Span == method.Span);

                    if (nodeToAnnotate != null)
                    {
                        var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(addition.Annotation);
                        currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "MethodMissingAttributeAnalysis",
                    Description = ex.Message,
                    OriginalCode = method.Identifier.Text,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    protected override CompilationUnitSyntax AnalyzeMethodSignatures(CompilationUnitSyntax root)
    {
        // First call base to handle async conversions
        var currentRoot = base.AnalyzeMethodSignatures(root);

        // Then handle lifecycle method visibility - find methods with SetUp/TearDown etc.
        // that are not public and need to be made public
        var lifecycleAttributeNames = new HashSet<string> { "SetUp", "TearDown", "OneTimeSetUp", "OneTimeTearDown" };
        var processedMethodSpans = new HashSet<Microsoft.CodeAnalysis.Text.TextSpan>();

        foreach (var method in OriginalRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            // Check if this method has a lifecycle attribute
            var hasLifecycleAttribute = method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => lifecycleAttributeNames.Contains(MigrationHelpers.GetAttributeName(attr)));

            if (!hasLifecycleAttribute) continue;

            // Check if already public
            if (method.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;

            // Check if we already processed this method
            if (processedMethodSpans.Contains(method.Span)) continue;
            processedMethodSpans.Add(method.Span);

            try
            {
                // Find the method in the current root
                var currentMethod = currentRoot.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Span == method.Span || m.Identifier.Text == method.Identifier.Text);

                if (currentMethod == null) continue;

                // Check if there's already a MethodSignatureChange for this method
                var existingChange = Plan.MethodSignatureChanges
                    .FirstOrDefault(c => currentMethod.HasAnnotation(c.Annotation));

                if (existingChange != null)
                {
                    // Update the existing change to also make public
                    // Since we can't modify the existing record, we need to remove and re-add
                    // Actually, this is tricky. Let's just add a new change if none exists.
                    // If there's an existing change, the transformer will handle both.
                    continue;
                }

                var change = new MethodSignatureChange
                {
                    MakePublic = true,
                    OriginalText = $"visibility:{method.Identifier.Text}"
                };
                Plan.MethodSignatureChanges.Add(change);

                var annotatedMethod = currentMethod.WithAdditionalAnnotations(change.Annotation);
                currentRoot = currentRoot.ReplaceNode(currentMethod, annotatedMethod);
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "LifecycleMethodVisibilityAnalysis",
                    Description = ex.Message,
                    OriginalCode = method.Identifier.Text,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private ParameterAttributeConversion? ConvertRangeAttribute(AttributeSyntax attr, ParameterSyntax parameter)
    {
        // [Range(1, 5)] -> [MatrixRange<int>(1, 5)]
        // [Range(1.0, 5.0)] -> [MatrixRange<double>(1.0, 5.0)]
        // [Range(1L, 100L)] -> [MatrixRange<long>(1L, 100L)]
        // [Range(1.0f, 5.0f)] -> [MatrixRange<float>(1.0f, 5.0f)]

        if (attr.ArgumentList?.Arguments.Count < 2)
            return null;

        // Determine the type from the first argument literal or the parameter type
        var firstArg = attr.ArgumentList.Arguments[0].Expression.ToString();
        var typeArg = InferRangeType(firstArg, parameter);

        return new ParameterAttributeConversion
        {
            NewAttributeName = $"MatrixRange<{typeArg}>",
            NewArgumentList = null, // Keep original arguments
            OriginalText = attr.ToString()
        };
    }

    private static string InferRangeType(string literal, ParameterSyntax parameter)
    {
        // Check for literal suffix
        if (literal.EndsWith("L", StringComparison.OrdinalIgnoreCase))
            return "long";
        if (literal.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            return "float";
        if (literal.EndsWith("d", StringComparison.OrdinalIgnoreCase) || literal.Contains("."))
            return "double";

        // Fall back to parameter type if available
        var paramType = parameter.Type?.ToString();
        if (!string.IsNullOrEmpty(paramType))
        {
            return paramType switch
            {
                "long" => "long",
                "float" => "float",
                "double" => "double",
                "decimal" => "decimal",
                "short" => "short",
                "byte" => "byte",
                _ => "int"
            };
        }

        return "int";
    }

    protected override bool ShouldRemoveBaseType(BaseTypeSyntax baseType)
    {
        // NUnit doesn't have common base types to remove like xUnit's IClassFixture
        return false;
    }

    protected override void AnalyzeUsings()
    {
        Plan.UsingPrefixesToRemove.Add("NUnit");
        // TUnit usings are handled automatically by MigrationHelpers
    }
}
