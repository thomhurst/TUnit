using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base.TwoPhase;

namespace TUnit.Analyzers.CodeFixers.TwoPhase;

/// <summary>
/// Phase 1 analyzer for xUnit to TUnit migration.
/// Collects all conversion targets while the semantic model is valid.
/// </summary>
public class XUnitTwoPhaseAnalyzer : MigrationAnalyzer
{
    private static readonly HashSet<string> XUnitAssertMethods = new()
    {
        "Equal", "NotEqual", "Same", "NotSame", "StrictEqual",
        "True", "False",
        "Null", "NotNull",
        "Empty", "NotEmpty", "Single", "Contains", "DoesNotContain", "All",
        "Throws", "ThrowsAsync", "ThrowsAny", "ThrowsAnyAsync",
        "IsType", "IsNotType", "IsAssignableFrom",
        "StartsWith", "EndsWith", "Matches", "DoesNotMatch",
        "InRange", "NotInRange",
        "Fail", "Skip", "Collection",
        "PropertyChanged", "PropertyChangedAsync",
        "Raises", "RaisesAsync", "RaisesAny", "RaisesAnyAsync",
        "Subset", "Superset", "ProperSubset", "ProperSuperset",
        "Distinct", "Equivalent"
    };

    // Track assertions that are assigned to variables (should not be converted)
    private readonly HashSet<InvocationExpressionSyntax> _variableAssignedAssertions = new();

    private static readonly HashSet<string> XUnitAttributeNames = new()
    {
        "Fact", "Theory", "InlineData", "MemberData", "ClassData",
        "Trait", "Collection", "CollectionDefinition"
    };

    private static readonly HashSet<string> XUnitRemovableAttributeNames = new()
    {
        // No longer removing Collection - we handle it specially now
    };

    private static readonly HashSet<string> XUnitBaseTypes = new()
    {
        "IClassFixture", "ICollectionFixture", "IAsyncLifetime"
    };

    public XUnitTwoPhaseAnalyzer(SemanticModel semanticModel, Compilation compilation)
        : base(semanticModel, compilation)
    {
    }

    protected override IEnumerable<InvocationExpressionSyntax> FindAssertionNodes(CompilationUnitSyntax root)
    {
        // First pass: detect assertions that are assigned to variables
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (IsXUnitAssertion(invocation) && IsAssignedToVariable(invocation))
            {
                _variableAssignedAssertions.Add(invocation);
            }
        }

        // Return all xUnit assertions (including variable-assigned ones, handled specially in AnalyzeAssertion)
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsXUnitAssertion);
    }

    private static bool IsAssignedToVariable(InvocationExpressionSyntax invocation)
    {
        // Check if this invocation is the right-hand side of a variable declaration
        // e.g., var ex = Assert.Throws<T>(...)
        var parent = invocation.Parent;

        // Check for variable declarator: var ex = Assert.Throws(...)
        if (parent is EqualsValueClauseSyntax equalsClause &&
            equalsClause.Parent is VariableDeclaratorSyntax)
        {
            return true;
        }

        // Check for assignment expression: ex = Assert.Throws(...)
        if (parent is AssignmentExpressionSyntax)
        {
            return true;
        }

        return false;
    }

    private bool IsXUnitAssertion(InvocationExpressionSyntax invocation)
    {
        // Check for Assert.X pattern
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Syntax-based check first (fast)
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" })
            {
                var methodName = memberAccess.Name.Identifier.Text;
                if (XUnitAssertMethods.Contains(methodName))
                {
                    // Semantic check to confirm it's xUnit
                    var symbolInfo = SemanticModel.GetSymbolInfo(invocation);
                    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                    {
                        var containingType = methodSymbol.ContainingType?.ToDisplayString();
                        return containingType?.StartsWith("Xunit.Assert") == true;
                    }
                }
            }
        }

        return false;
    }

    protected override AssertionConversion? AnalyzeAssertion(InvocationExpressionSyntax node)
    {
        if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        var methodName = memberAccess.Name.Identifier.Text;
        var arguments = node.ArgumentList.Arguments;

        // Skip Throws/ThrowsAsync if assigned to a variable (result is used)
        if ((methodName is "Throws" or "ThrowsAsync" or "ThrowsAny" or "ThrowsAnyAsync") &&
            _variableAssignedAssertions.Contains(node))
        {
            return null; // Don't convert - keep as-is
        }

        var (kind, replacementCode, introducesAwait, todoComment) = methodName switch
        {
            "Equal" => ConvertEqual(arguments),
            "NotEqual" => ConvertNotEqual(arguments),
            "True" => ConvertTrue(arguments),
            "False" => ConvertFalse(arguments),
            "Null" => ConvertNull(arguments),
            "NotNull" => ConvertNotNull(arguments),
            "Same" => ConvertSame(arguments),
            "NotSame" => ConvertNotSame(arguments),
            "StrictEqual" => ConvertStrictEqual(arguments),
            "Empty" => ConvertEmpty(arguments),
            "NotEmpty" => ConvertNotEmpty(arguments),
            "Single" => ConvertSingle(arguments),
            "Contains" => ConvertContains(arguments),
            "DoesNotContain" => ConvertDoesNotContain(arguments),
            "Throws" => ConvertThrows(memberAccess, arguments),
            "ThrowsAsync" => ConvertThrowsAsync(memberAccess, arguments),
            "ThrowsAny" => ConvertThrowsAny(memberAccess, arguments),
            "ThrowsAnyAsync" => ConvertThrowsAnyAsync(memberAccess, arguments),
            "IsType" => ConvertIsType(memberAccess, arguments),
            "IsNotType" => ConvertIsNotType(memberAccess, arguments),
            "IsAssignableFrom" => ConvertIsAssignableFrom(memberAccess, arguments),
            "StartsWith" => ConvertStartsWith(arguments),
            "EndsWith" => ConvertEndsWith(arguments),
            "Matches" => ConvertMatches(arguments),
            "DoesNotMatch" => ConvertDoesNotMatch(arguments),
            "InRange" => ConvertInRange(arguments),
            "NotInRange" => ConvertNotInRange(arguments),
            "Fail" => (AssertionConversionKind.Fail, "Assert.Fail()", false, (string?)null),
            "All" => ConvertAll(arguments),
            "Collection" => ConvertCollection(arguments),
            "Subset" => ConvertSubset(arguments),
            "Superset" => ConvertSuperset(arguments),
            "ProperSubset" => ConvertProperSubset(arguments),
            "ProperSuperset" => ConvertProperSuperset(arguments),
            "Distinct" => ConvertDistinct(arguments),
            "Equivalent" => ConvertEquivalent(arguments),
            _ => (AssertionConversionKind.Unknown, null, false, (string?)null)
        };

        if (replacementCode == null)
            return null;

        return new AssertionConversion
        {
            Kind = kind,
            ReplacementCode = replacementCode,
            IntroducesAwait = introducesAwait,
            TodoComment = todoComment,
            OriginalText = node.ToString()
        };
    }

    #region Assertion Conversions

    private (AssertionConversionKind, string?, bool, string?) ConvertEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Equal, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.Equal, $"await Assert.That({actual}).IsEqualTo({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertNotEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.NotEqual, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.NotEqual, $"await Assert.That({actual}).IsNotEqualTo({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertTrue(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.True, null, false, null);

        var condition = args[0].Expression.ToString();
        var message = GetMessageArgument(args, 1);
        var assertion = $"await Assert.That({condition}).IsTrue()";
        if (message != null)
        {
            assertion += $".Because({message})";
        }
        return (AssertionConversionKind.True, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertFalse(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.False, null, false, null);

        var condition = args[0].Expression.ToString();
        var message = GetMessageArgument(args, 1);
        var assertion = $"await Assert.That({condition}).IsFalse()";
        if (message != null)
        {
            assertion += $".Because({message})";
        }
        return (AssertionConversionKind.False, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertNull(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.Null, null, false, null);

        var value = args[0].Expression.ToString();
        return (AssertionConversionKind.Null, $"await Assert.That({value}).IsNull()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertNotNull(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.NotNull, null, false, null);

        var value = args[0].Expression.ToString();
        return (AssertionConversionKind.NotNull, $"await Assert.That({value}).IsNotNull()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertSame(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Same, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.Same, $"await Assert.That({actual}).IsSameReferenceAs({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertNotSame(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.NotSame, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.NotSame, $"await Assert.That({actual}).IsNotSameReferenceAs({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStrictEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.StrictEqual, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.StrictEqual, $"await Assert.That({actual}).IsStrictlyEqualTo({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertEmpty(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.Empty, null, false, null);

        var collection = args[0].Expression.ToString();
        return (AssertionConversionKind.Empty, $"await Assert.That({collection}).IsEmpty()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertNotEmpty(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.NotEmpty, null, false, null);

        var collection = args[0].Expression.ToString();
        return (AssertionConversionKind.NotEmpty, $"await Assert.That({collection}).IsNotEmpty()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertSingle(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.Single, null, false, null);

        var collection = args[0].Expression.ToString();
        return (AssertionConversionKind.Single, $"await Assert.That({collection}).HasSingleItem()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertContains(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Contains, null, false, null);

        var expected = args[0].Expression.ToString();
        var collection = args[1].Expression.ToString();

        return (AssertionConversionKind.Contains, $"await Assert.That({collection}).Contains({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertDoesNotContain(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.DoesNotContain, null, false, null);

        var expected = args[0].Expression.ToString();
        var collection = args[1].Expression.ToString();

        return (AssertionConversionKind.DoesNotContain, $"await Assert.That({collection}).DoesNotContain({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertThrows(MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        // TUnit has Assert.Throws<T>(action) with the same API as xUnit - no conversion needed!
        // TUnit's Assert.Throws returns TException just like xUnit.
        return (AssertionConversionKind.Throws, null, false, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertThrowsAsync(MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        // TUnit has Assert.ThrowsAsync<T>(action) with similar API to xUnit - no conversion needed!
        // TUnit's Assert.ThrowsAsync returns ThrowsAssertion<T> which is awaitable like xUnit's Task<T>.
        return (AssertionConversionKind.ThrowsAsync, null, false, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertThrowsAny(MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.ThrowsAny, null, false, null);

        var typeArg = GetGenericTypeArgument(memberAccess);
        var action = args[0].Expression.ToString();

        if (typeArg != null)
        {
            return (AssertionConversionKind.ThrowsAny, $"await Assert.That({action}).Throws<{typeArg}>()", true, null);
        }

        return (AssertionConversionKind.ThrowsAny, $"await Assert.That({action}).ThrowsException()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertThrowsAnyAsync(MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.ThrowsAnyAsync, null, false, null);

        var typeArg = GetGenericTypeArgument(memberAccess);
        var action = args[0].Expression.ToString();

        if (typeArg != null)
        {
            return (AssertionConversionKind.ThrowsAnyAsync, $"await Assert.That({action}).Throws<{typeArg}>()", true, null);
        }

        return (AssertionConversionKind.ThrowsAnyAsync, $"await Assert.That({action}).ThrowsException()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsType(MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsType, null, false, null);

        var typeArg = GetGenericTypeArgument(memberAccess);
        var value = args[0].Expression.ToString();

        if (typeArg != null)
        {
            return (AssertionConversionKind.IsType, $"await Assert.That({value}).IsTypeOf<{typeArg}>()", true, null);
        }

        return (AssertionConversionKind.IsType, null, false, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsNotType(MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsNotType, null, false, null);

        var typeArg = GetGenericTypeArgument(memberAccess);
        var value = args[0].Expression.ToString();

        if (typeArg != null)
        {
            return (AssertionConversionKind.IsNotType, $"await Assert.That({value}).IsNotTypeOf<{typeArg}>()", true, null);
        }

        return (AssertionConversionKind.IsNotType, null, false, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsAssignableFrom(MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsAssignableFrom, null, false, null);

        var typeArg = GetGenericTypeArgument(memberAccess);
        var value = args[0].Expression.ToString();

        if (typeArg != null)
        {
            return (AssertionConversionKind.IsAssignableFrom, $"await Assert.That({value}).IsAssignableTo<{typeArg}>()", true, null);
        }

        return (AssertionConversionKind.IsAssignableFrom, null, false, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStartsWith(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.StartsWith, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.StartsWith, $"await Assert.That({actual}).StartsWith({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertEndsWith(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.EndsWith, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.EndsWith, $"await Assert.That({actual}).EndsWith({expected})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertMatches(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Matches, null, false, null);

        var pattern = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.Matches, $"await Assert.That({actual}).Matches({pattern})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertInRange(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 3) return (AssertionConversionKind.InRange, null, false, null);

        var actual = args[0].Expression.ToString();
        var low = args[1].Expression.ToString();
        var high = args[2].Expression.ToString();

        return (AssertionConversionKind.InRange, $"await Assert.That({actual}).IsInRange({low},{high})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertNotInRange(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 3) return (AssertionConversionKind.NotInRange, null, false, null);

        var actual = args[0].Expression.ToString();
        var low = args[1].Expression.ToString();
        var high = args[2].Expression.ToString();

        return (AssertionConversionKind.NotInRange, $"await Assert.That({actual}).IsNotInRange({low},{high})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertAll(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.All, null, false, null);

        var collection = args[0].Expression.ToString();
        var actionExpression = args[1].Expression;

        // Try to extract predicate from Assert.True/False patterns
        var predicate = TryConvertActionToPredicate(actionExpression);

        return (AssertionConversionKind.All, $"await Assert.That({collection}).All({predicate})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollection(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.Collection, null, false, null);

        var collection = args[0].Expression.ToString();
        // Count the element inspectors (args after the first one)
        var inspectorCount = args.Count - 1;

        var todoComment = "// TODO: TUnit migration - Assert.Collection had element inspectors. Manually add assertions for each element.";
        return (AssertionConversionKind.Collection, $"await Assert.That({collection}).HasCount({inspectorCount})", true, todoComment);
    }

    private string TryConvertActionToPredicate(ExpressionSyntax actionExpression)
    {
        // Try to convert xUnit action patterns to TUnit predicates
        // Pattern: item => Assert.True(item > 0) -> item => item > 0
        // Pattern: item => Assert.False(item < 0) -> item => !(item < 0)

        if (actionExpression is SimpleLambdaExpressionSyntax simpleLambda)
        {
            var parameter = simpleLambda.Parameter.Identifier.Text;
            var body = simpleLambda.Body;

            // Check if body is an xUnit assertion invocation
            if (body is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" })
            {
                var methodName = memberAccess.Name.Identifier.Text;
                var invocationArgs = invocation.ArgumentList.Arguments;

                var predicateBody = methodName switch
                {
                    "True" when invocationArgs.Count >= 1 => invocationArgs[0].Expression.ToString(),
                    "False" when invocationArgs.Count >= 1 => $"!({invocationArgs[0].Expression})",
                    "NotNull" when invocationArgs.Count >= 1 => $"{invocationArgs[0].Expression} != null",
                    "Null" when invocationArgs.Count >= 1 => $"{invocationArgs[0].Expression} == null",
                    _ => null
                };

                if (predicateBody != null)
                {
                    return $"{parameter} => {predicateBody}";
                }
            }
        }
        else if (actionExpression is ParenthesizedLambdaExpressionSyntax parenLambda &&
                 parenLambda.ParameterList.Parameters.Count == 1)
        {
            var parameter = parenLambda.ParameterList.Parameters[0].Identifier.Text;
            var body = parenLambda.Body;

            if (body is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" })
            {
                var methodName = memberAccess.Name.Identifier.Text;
                var invocationArgs = invocation.ArgumentList.Arguments;

                var predicateBody = methodName switch
                {
                    "True" when invocationArgs.Count >= 1 => invocationArgs[0].Expression.ToString(),
                    "False" when invocationArgs.Count >= 1 => $"!({invocationArgs[0].Expression})",
                    "NotNull" when invocationArgs.Count >= 1 => $"{invocationArgs[0].Expression} != null",
                    "Null" when invocationArgs.Count >= 1 => $"{invocationArgs[0].Expression} == null",
                    _ => null
                };

                if (predicateBody != null)
                {
                    return $"{parameter} => {predicateBody}";
                }
            }
        }

        // Fallback: return the action as-is (may not work, but better than nothing)
        return actionExpression.ToString();
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertDoesNotMatch(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Matches, null, false, null);

        var pattern = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.Matches, $"await Assert.That({actual}).DoesNotMatch({pattern})", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertSubset(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Contains, null, false, null);

        var superset = args[0].Expression.ToString();
        var subset = args[1].Expression.ToString();

        return (AssertionConversionKind.Contains, $"await Assert.That({superset}).Contains({subset}).IgnoringOrder()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertSuperset(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Contains, null, false, null);

        var subset = args[0].Expression.ToString();
        var superset = args[1].Expression.ToString();

        return (AssertionConversionKind.Contains, $"await Assert.That({superset}).Contains({subset}).IgnoringOrder()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertProperSubset(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Contains, null, false, null);

        // xUnit: Assert.ProperSubset(expectedSuperset, actual)
        // actual should be a proper subset of expectedSuperset
        // TUnit: Assert.That(first_arg).IsSubsetOf(second_arg) with TODO comment
        var first = args[0].Expression.ToString();
        var second = args[1].Expression.ToString();

        var todoComment = "// TODO: TUnit migration - ProperSubset requires strict subset (not equal). Add additional assertion if needed.";
        return (AssertionConversionKind.Contains, $"await Assert.That({first}).IsSubsetOf({second})", true, todoComment);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertProperSuperset(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Contains, null, false, null);

        // xUnit: Assert.ProperSuperset(expectedSubset, actual)
        // actual should be a proper superset of expectedSubset
        // TUnit: Assert.That(first_arg).IsSupersetOf(second_arg) with TODO comment
        var first = args[0].Expression.ToString();
        var second = args[1].Expression.ToString();

        var todoComment = "// TODO: TUnit migration - ProperSuperset requires strict superset (not equal). Add additional assertion if needed.";
        return (AssertionConversionKind.Contains, $"await Assert.That({first}).IsSupersetOf({second})", true, todoComment);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertDistinct(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.Collection, null, false, null);

        var collection = args[0].Expression.ToString();

        return (AssertionConversionKind.Collection, $"await Assert.That({collection}).HasDistinctItems()", true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertEquivalent(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.Equal, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        return (AssertionConversionKind.Equal, $"await Assert.That({actual}).IsEquivalentTo({expected})", true, null);
    }

    private static string? GetGenericTypeArgument(MemberAccessExpressionSyntax memberAccess)
    {
        if (memberAccess.Name is GenericNameSyntax genericName)
        {
            return genericName.TypeArgumentList.Arguments.FirstOrDefault()?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Gets the message argument from xUnit assertions if present.
    /// In xUnit, message is typically the last string argument.
    /// Returns the full argument syntax (e.g., "\"my message\"") for use with .Because().
    /// </summary>
    private static string? GetMessageArgument(SeparatedSyntaxList<ArgumentSyntax> args, int startIndex)
    {
        // Look for a string literal argument at or after the startIndex
        for (int i = startIndex; i < args.Count; i++)
        {
            var arg = args[i];
            // Named argument check (e.g., userMessage: "...")
            if (arg.NameColon?.Name.Identifier.Text is "userMessage" or "message")
            {
                return arg.Expression.ToString();
            }
            // String literal check
            if (arg.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return arg.Expression.ToString();
            }
            // Interpolated string check
            if (arg.Expression is InterpolatedStringExpressionSyntax)
            {
                return arg.Expression.ToString();
            }
        }
        return null;
    }

    #endregion

    #region Attribute Analysis

    protected override bool ShouldRemoveAttribute(AttributeSyntax node)
    {
        var name = GetAttributeName(node);
        return XUnitRemovableAttributeNames.Contains(name);
    }

    protected override AttributeConversion? AnalyzeAttribute(AttributeSyntax node)
    {
        var name = GetAttributeName(node);

        if (!XUnitAttributeNames.Contains(name))
            return null;

        var conversion = name switch
        {
            "Fact" or "FactAttribute" => ConvertTestAttribute(node),
            "Theory" or "TheoryAttribute" => ConvertTestAttribute(node),
            "Trait" or "TraitAttribute" => new AttributeConversion
            {
                NewAttributeName = "Property",
                NewArgumentList = null, // Keep original arguments
                OriginalText = node.ToString()
            },
            "InlineData" or "InlineDataAttribute" => new AttributeConversion
            {
                NewAttributeName = "Arguments",
                NewArgumentList = null, // Keep original arguments
                OriginalText = node.ToString()
            },
            "MemberData" or "MemberDataAttribute" => ConvertMemberData(node),
            "ClassData" or "ClassDataAttribute" => ConvertClassData(node),
            "Collection" or "CollectionAttribute" => ConvertCollection(node),
            "CollectionDefinition" or "CollectionDefinitionAttribute" => new AttributeConversion
            {
                NewAttributeName = "System.Obsolete",
                NewArgumentList = "", // Remove arguments
                OriginalText = node.ToString()
            },
            _ => null
        };

        return conversion;
    }

    private AttributeConversion ConvertTestAttribute(AttributeSyntax node)
    {
        // Check for Skip argument: [Fact(Skip = "reason")] or [Theory(Skip = "reason")]
        var skipArg = node.ArgumentList?.Arguments
            .FirstOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == "Skip");

        if (skipArg != null)
        {
            // Extract the skip reason and create additional Skip attribute
            var skipReason = skipArg.Expression.ToString();
            return new AttributeConversion
            {
                NewAttributeName = "Test",
                NewArgumentList = "", // Remove the Skip argument
                OriginalText = node.ToString(),
                AdditionalAttributes = new List<AdditionalAttribute>
                {
                    new AdditionalAttribute
                    {
                        Name = "Skip",
                        Arguments = $"({skipReason})"
                    }
                }
            };
        }

        return new AttributeConversion
        {
            NewAttributeName = "Test",
            NewArgumentList = "", // Remove any arguments
            OriginalText = node.ToString()
        };
    }

    private AttributeConversion? ConvertCollection(AttributeSyntax node)
    {
        // [Collection("name")] on a test class needs to:
        // 1. Find the CollectionDefinition class with matching name
        // 2. Check if it has DisableParallelization = true (add [NotInParallel])
        // 3. Find ICollectionFixture<T> interface and add ClassDataSource<T>(Shared = SharedType.Keyed, Key = "name")

        var collectionNameArg = node.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
        if (collectionNameArg == null)
            return null;

        var collectionName = collectionNameArg.ToString().Trim('"');

        // Find the CollectionDefinition class
        var collectionDefinition = FindCollectionDefinition(collectionName);
        if (collectionDefinition == null)
        {
            // No CollectionDefinition found - just remove the attribute
            return new AttributeConversion
            {
                NewAttributeName = "System.Obsolete",
                NewArgumentList = "",
                OriginalText = node.ToString()
            };
        }

        // Check for DisableParallelization
        var disableParallelization = HasDisableParallelization(collectionDefinition);

        // Find ICollectionFixture<T> interface
        var fixtureType = GetCollectionFixtureType(collectionDefinition);

        var additionalAttributes = new List<AdditionalAttribute>();

        // Add NotInParallel if DisableParallelization = true
        if (disableParallelization)
        {
            additionalAttributes.Add(new AdditionalAttribute
            {
                Name = "NotInParallel",
                Arguments = null
            });
        }

        // If there's a fixture type, use ClassDataSource
        if (fixtureType != null)
        {
            // The conversion produces ClassDataSource<T>(Shared = SharedType.Keyed, Key = "name")
            // We need to return this as the primary attribute conversion
            var keyArgument = collectionNameArg.ToString(); // Keep the original string including quotes

            return new AttributeConversion
            {
                NewAttributeName = $"ClassDataSource<{fixtureType}>",
                NewArgumentList = $"(Shared = SharedType.Keyed, Key = {keyArgument})",
                OriginalText = node.ToString(),
                AdditionalAttributes = additionalAttributes.Count > 0 ? additionalAttributes : null
            };
        }
        else if (disableParallelization)
        {
            // No fixture, just add NotInParallel
            return new AttributeConversion
            {
                NewAttributeName = "NotInParallel",
                NewArgumentList = "",
                OriginalText = node.ToString()
            };
        }

        // No fixture, no parallelization disable - just remove the attribute
        return new AttributeConversion
        {
            NewAttributeName = "System.Obsolete",
            NewArgumentList = "",
            OriginalText = node.ToString()
        };
    }

    private ClassDeclarationSyntax? FindCollectionDefinition(string collectionName)
    {
        // Search all classes in the compilation for [CollectionDefinition("name")]
        foreach (var tree in Compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                foreach (var attrList in classDecl.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrName = GetAttributeName(attr);
                        if (attrName == "CollectionDefinition" || attrName == "CollectionDefinitionAttribute")
                        {
                            var nameArg = attr.ArgumentList?.Arguments.FirstOrDefault()?.Expression?.ToString()?.Trim('"');
                            if (nameArg == collectionName)
                            {
                                return classDecl;
                            }
                        }
                    }
                }
            }
        }
        return null;
    }

    private bool HasDisableParallelization(ClassDeclarationSyntax collectionDefinition)
    {
        // Check for [CollectionDefinition("name", DisableParallelization = true)]
        foreach (var attrList in collectionDefinition.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = GetAttributeName(attr);
                if (attrName == "CollectionDefinition" || attrName == "CollectionDefinitionAttribute")
                {
                    var disableArg = attr.ArgumentList?.Arguments
                        .FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == "DisableParallelization");
                    if (disableArg?.Expression is LiteralExpressionSyntax literal &&
                        literal.Token.ValueText == "true")
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private string? GetCollectionFixtureType(ClassDeclarationSyntax collectionDefinition)
    {
        // Find ICollectionFixture<T> in the base list
        if (collectionDefinition.BaseList == null)
            return null;

        foreach (var baseType in collectionDefinition.BaseList.Types)
        {
            var typeName = baseType.Type.ToString();
            if (typeName.StartsWith("ICollectionFixture<"))
            {
                // Extract the type argument
                if (baseType.Type is GenericNameSyntax genericName)
                {
                    return genericName.TypeArgumentList.Arguments.FirstOrDefault()?.ToString();
                }
            }
        }

        return null;
    }

    private AttributeConversion? ConvertMemberData(AttributeSyntax node)
    {
        // [MemberData(nameof(Data))] -> [MethodDataSource(nameof(Data))]
        // [MemberData(nameof(Data), MemberType = typeof(Foo))] -> [MethodDataSource(typeof(Foo), nameof(Data))]
        var args = node.ArgumentList?.Arguments;
        if (args == null || args.Value.Count == 0)
            return null;

        var memberName = args.Value[0].Expression.ToString();

        // Check for MemberType named argument
        var memberTypeArg = args.Value.Skip(1)
            .FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == "MemberType");

        if (memberTypeArg != null)
        {
            var memberType = memberTypeArg.Expression.ToString();
            return new AttributeConversion
            {
                NewAttributeName = "MethodDataSource",
                NewArgumentList = $"({memberType}, {memberName})",
                OriginalText = node.ToString()
            };
        }

        return new AttributeConversion
        {
            NewAttributeName = "MethodDataSource",
            NewArgumentList = $"({memberName})",
            OriginalText = node.ToString()
        };
    }

    private AttributeConversion? ConvertClassData(AttributeSyntax node)
    {
        // [ClassData(typeof(TestDataGenerator))] -> [MethodDataSource(typeof(TestDataGenerator), "GetEnumerator")]
        var args = node.ArgumentList?.Arguments;
        if (args == null || args.Value.Count == 0)
            return null;

        var typeArg = args.Value[0].Expression.ToString();

        return new AttributeConversion
        {
            NewAttributeName = "MethodDataSource",
            NewArgumentList = $"({typeArg}, \"GetEnumerator\")",
            OriginalText = node.ToString()
        };
    }

    private static string GetAttributeName(AttributeSyntax attribute)
    {
        return attribute.Name switch
        {
            SimpleNameSyntax simpleName => simpleName.Identifier.Text,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.Text,
            _ => ""
        };
    }

    #endregion

    #region Base Type Analysis

    protected override CompilationUnitSyntax AnalyzeBaseTypes(CompilationUnitSyntax root)
    {
        var classNodes = OriginalRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        var currentRoot = root;

        foreach (var classNode in classNodes)
        {
            if (classNode.BaseList == null) continue;

            var hasIAsyncLifetime = false;
            var classFixtureTypes = new List<string>();

            foreach (var originalBaseType in classNode.BaseList.Types)
            {
                try
                {
                    var typeName = originalBaseType.Type.ToString();

                    // Check for IAsyncLifetime
                    if (typeName == "IAsyncLifetime" || IsIAsyncLifetimeType(originalBaseType))
                    {
                        hasIAsyncLifetime = true;
                    }

                    // Check for IClassFixture<T>
                    if (typeName.StartsWith("IClassFixture<") || IsIClassFixtureType(originalBaseType))
                    {
                        var fixtureType = ExtractGenericTypeArgument(originalBaseType);
                        if (fixtureType != null)
                        {
                            classFixtureTypes.Add(fixtureType);
                        }
                    }

                    if (ShouldRemoveBaseType(originalBaseType))
                    {
                        var removal = new BaseTypeRemoval
                        {
                            TypeName = typeName,
                            OriginalText = originalBaseType.ToString()
                        };
                        Plan.BaseTypeRemovals.Add(removal);

                        var nodeToAnnotate = currentRoot.DescendantNodes()
                            .OfType<BaseTypeSyntax>()
                            .FirstOrDefault(n => n.Span == originalBaseType.Span);

                        if (nodeToAnnotate != null)
                        {
                            var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(removal.Annotation);
                            currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plan.Failures.Add(new ConversionFailure
                    {
                        Phase = "BaseTypeAnalysis",
                        Description = ex.Message,
                        OriginalCode = originalBaseType.ToString(),
                        Exception = ex
                    });
                }
            }

            // If this class implements IAsyncLifetime, handle based on whether it's a test class
            if (hasIAsyncLifetime)
            {
                try
                {
                    var isTestClass = HasTestMethods(classNode);

                    if (isTestClass)
                    {
                        // Test class: Add [Before(Test)]/[After(Test)] attributes
                        currentRoot = AnalyzeLifecycleMethods(currentRoot, classNode);
                    }
                    else
                    {
                        // Non-test class: Add IAsyncInitializer and IAsyncDisposable interfaces
                        currentRoot = AnalyzeNonTestLifecycleMethods(currentRoot, classNode);
                    }
                }
                catch (Exception ex)
                {
                    Plan.Failures.Add(new ConversionFailure
                    {
                        Phase = "LifecycleMethodAnalysis",
                        Description = ex.Message,
                        OriginalCode = classNode.Identifier.Text,
                        Exception = ex
                    });
                }
            }

            // If this class implements IClassFixture<T>, add ClassDataSource attribute
            foreach (var fixtureType in classFixtureTypes)
            {
                try
                {
                    var classAttrAddition = new ClassAttributeAddition
                    {
                        AttributeCode = $"ClassDataSource<{fixtureType}>(Shared = SharedType.PerClass)",
                        OriginalText = classNode.Identifier.Text
                    };
                    Plan.ClassAttributeAdditions.Add(classAttrAddition);

                    // Find the class in current root and annotate
                    var classToAnnotate = currentRoot.DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault(c => c.Span == classNode.Span);

                    if (classToAnnotate != null)
                    {
                        var annotatedClass = classToAnnotate.WithAdditionalAnnotations(classAttrAddition.Annotation);
                        currentRoot = currentRoot.ReplaceNode(classToAnnotate, annotatedClass);
                    }
                }
                catch (Exception ex)
                {
                    Plan.Failures.Add(new ConversionFailure
                    {
                        Phase = "ClassAttributeAddition",
                        Description = ex.Message,
                        OriginalCode = classNode.Identifier.Text,
                        Exception = ex
                    });
                }
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax AnalyzeLifecycleMethods(CompilationUnitSyntax root, ClassDeclarationSyntax originalClass)
    {
        var currentRoot = root;

        // Find InitializeAsync and DisposeAsync methods in the original class
        foreach (var method in originalClass.Members.OfType<MethodDeclarationSyntax>())
        {
            try
            {
                var methodName = method.Identifier.Text;

                if (methodName == "InitializeAsync")
                {
                    // Add [Before(Test)] attribute and change return type to Task
                    var methodAttrAddition = new MethodAttributeAddition
                    {
                        AttributeCode = "Before(Test)",
                        NewReturnType = method.ReturnType.ToString() == "ValueTask" ? "Task" : null,
                        OriginalText = method.Identifier.Text
                    };
                    Plan.MethodAttributeAdditions.Add(methodAttrAddition);

                    // Find method in current root and annotate
                    var methodToAnnotate = currentRoot.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(m => m.Span == method.Span);

                    if (methodToAnnotate != null)
                    {
                        var annotatedMethod = methodToAnnotate.WithAdditionalAnnotations(methodAttrAddition.Annotation);
                        currentRoot = currentRoot.ReplaceNode(methodToAnnotate, annotatedMethod);
                    }
                }
                else if (methodName == "DisposeAsync")
                {
                    // Add [After(Test)] attribute and change return type to Task
                    var methodAttrAddition = new MethodAttributeAddition
                    {
                        AttributeCode = "After(Test)",
                        NewReturnType = method.ReturnType.ToString() == "ValueTask" ? "Task" : null,
                        OriginalText = method.Identifier.Text
                    };
                    Plan.MethodAttributeAdditions.Add(methodAttrAddition);

                    // Find method in current root and annotate
                    var methodToAnnotate = currentRoot.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(m => m.Span == method.Span);

                    if (methodToAnnotate != null)
                    {
                        var annotatedMethod = methodToAnnotate.WithAdditionalAnnotations(methodAttrAddition.Annotation);
                        currentRoot = currentRoot.ReplaceNode(methodToAnnotate, annotatedMethod);
                    }
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "LifecycleMethodAnalysis",
                    Description = ex.Message,
                    OriginalCode = method.Identifier.Text,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private bool HasTestMethods(ClassDeclarationSyntax classNode)
    {
        // Check if any method in the class has test-related attributes
        foreach (var method in classNode.Members.OfType<MethodDeclarationSyntax>())
        {
            foreach (var attrList in method.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var attrName = GetAttributeName(attr);
                    if (attrName is "Fact" or "FactAttribute" or "Theory" or "TheoryAttribute" or
                        "Test" or "TestAttribute")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private CompilationUnitSyntax AnalyzeNonTestLifecycleMethods(CompilationUnitSyntax root, ClassDeclarationSyntax originalClass)
    {
        var currentRoot = root;

        // For non-test classes implementing IAsyncLifetime, we need to:
        // 1. Add IAsyncInitializer and IAsyncDisposable interfaces
        // 2. Change InitializeAsync return type from ValueTask to Task

        // Add base types for the class
        var classToAnnotate = currentRoot.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Span == originalClass.Span);

        if (classToAnnotate != null)
        {
            // Add IAsyncInitializer base type
            var initializerAddition = new BaseTypeAddition
            {
                TypeName = "IAsyncInitializer",
                OriginalText = originalClass.Identifier.Text
            };
            Plan.BaseTypeAdditions.Add(initializerAddition);

            var annotatedClass = classToAnnotate.WithAdditionalAnnotations(initializerAddition.Annotation);
            currentRoot = currentRoot.ReplaceNode(classToAnnotate, annotatedClass);

            // Find class again after modification
            classToAnnotate = currentRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.HasAnnotation(initializerAddition.Annotation));

            if (classToAnnotate != null)
            {
                // Add IAsyncDisposable base type
                var disposableAddition = new BaseTypeAddition
                {
                    TypeName = "IAsyncDisposable",
                    OriginalText = originalClass.Identifier.Text
                };
                Plan.BaseTypeAdditions.Add(disposableAddition);

                annotatedClass = classToAnnotate.WithAdditionalAnnotations(disposableAddition.Annotation);
                currentRoot = currentRoot.ReplaceNode(classToAnnotate, annotatedClass);
            }
        }

        // Change InitializeAsync return type from ValueTask to Task
        foreach (var method in originalClass.Members.OfType<MethodDeclarationSyntax>())
        {
            if (method.Identifier.Text == "InitializeAsync" &&
                method.ReturnType.ToString() == "ValueTask")
            {
                var signatureChange = new MethodSignatureChange
                {
                    ChangeValueTaskToTask = true,
                    OriginalText = method.Identifier.Text
                };
                Plan.MethodSignatureChanges.Add(signatureChange);

                var methodToAnnotate = currentRoot.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Span == method.Span);

                if (methodToAnnotate != null)
                {
                    var annotatedMethod = methodToAnnotate.WithAdditionalAnnotations(signatureChange.Annotation);
                    currentRoot = currentRoot.ReplaceNode(methodToAnnotate, annotatedMethod);
                }
            }
        }

        return currentRoot;
    }

    private bool IsIAsyncLifetimeType(BaseTypeSyntax baseType)
    {
        var typeInfo = SemanticModel.GetTypeInfo(baseType.Type);
        return typeInfo.Type?.ToDisplayString() == "Xunit.IAsyncLifetime";
    }

    private bool IsIClassFixtureType(BaseTypeSyntax baseType)
    {
        var typeInfo = SemanticModel.GetTypeInfo(baseType.Type);
        var displayString = typeInfo.Type?.ToDisplayString() ?? "";
        return displayString.StartsWith("Xunit.IClassFixture<");
    }

    private string? ExtractGenericTypeArgument(BaseTypeSyntax baseType)
    {
        if (baseType.Type is GenericNameSyntax genericName)
        {
            return genericName.TypeArgumentList.Arguments.FirstOrDefault()?.ToString();
        }

        // Try semantic model for qualified names
        var typeInfo = SemanticModel.GetTypeInfo(baseType.Type);
        if (typeInfo.Type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        return null;
    }

    protected override bool ShouldRemoveBaseType(BaseTypeSyntax baseType)
    {
        var typeName = baseType.Type.ToString();

        // Check for generic interface patterns like IClassFixture<T>
        foreach (var xunitType in XUnitBaseTypes)
        {
            if (typeName.StartsWith(xunitType + "<") || typeName == xunitType)
            {
                return true;
            }
        }

        // Semantic check for xUnit types
        var typeInfo = SemanticModel.GetTypeInfo(baseType.Type);
        if (typeInfo.Type != null)
        {
            var ns = typeInfo.Type.ContainingNamespace?.ToDisplayString();
            if (ns?.StartsWith("Xunit") == true)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Member Analysis

    protected override CompilationUnitSyntax AnalyzeMembers(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Find ITestOutputHelper fields and properties on ORIGINAL tree (for semantic analysis)
        var members = OriginalRoot.DescendantNodes()
            .Where(n =>
                (n is PropertyDeclarationSyntax prop && IsTestOutputHelperType(prop.Type)) ||
                (n is FieldDeclarationSyntax field && IsTestOutputHelperType(field.Declaration.Type)))
            .ToList();

        foreach (var originalMember in members)
        {
            try
            {
                var removal = new MemberRemoval
                {
                    MemberName = GetMemberName(originalMember),
                    OriginalText = originalMember.ToString()
                };

                Plan.MemberRemovals.Add(removal);

                // Find corresponding node in current tree by span
                var nodeToAnnotate = currentRoot.DescendantNodes()
                    .FirstOrDefault(n => n.Span == originalMember.Span);

                if (nodeToAnnotate != null)
                {
                    var annotatedMember = nodeToAnnotate.WithAdditionalAnnotations(removal.Annotation);
                    currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedMember);
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "MemberAnalysis",
                    Description = ex.Message,
                    OriginalCode = originalMember.ToString(),
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private bool IsTestOutputHelperType(TypeSyntax type)
    {
        var typeName = type.ToString();
        if (typeName == "ITestOutputHelper")
            return true;

        // Use semantic model on type from ORIGINAL tree
        var typeInfo = SemanticModel.GetTypeInfo(type);
        return typeInfo.Type?.ToDisplayString() == "Xunit.Abstractions.ITestOutputHelper";
    }

    private static string GetMemberName(SyntaxNode member)
    {
        return member switch
        {
            PropertyDeclarationSyntax prop => prop.Identifier.Text,
            FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "",
            _ => ""
        };
    }

    #endregion

    #region Constructor Parameter Analysis

    protected override CompilationUnitSyntax AnalyzeConstructorParameters(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Find parameters on ORIGINAL tree (for semantic analysis)
        var parameters = OriginalRoot.DescendantNodes()
            .OfType<ParameterSyntax>()
            .Where(p => p.Type != null && IsTestOutputHelperType(p.Type))
            .ToList();

        foreach (var originalParam in parameters)
        {
            try
            {
                var removal = new ConstructorParameterRemoval
                {
                    ParameterName = originalParam.Identifier.Text,
                    ParameterType = originalParam.Type?.ToString() ?? "",
                    OriginalText = originalParam.ToString()
                };

                Plan.ConstructorParameterRemovals.Add(removal);

                // Find corresponding node in current tree by span
                var nodeToAnnotate = currentRoot.DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .FirstOrDefault(p => p.Span == originalParam.Span);

                if (nodeToAnnotate != null)
                {
                    var annotatedParam = nodeToAnnotate.WithAdditionalAnnotations(removal.Annotation);
                    currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedParam);
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "ConstructorParameterAnalysis",
                    Description = ex.Message,
                    OriginalCode = originalParam.ToString(),
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    #endregion

    protected override void AnalyzeUsings()
    {
        Plan.UsingPrefixesToRemove.Add("Xunit");
        // TUnit usings are handled automatically by MigrationHelpers
    }

    #region Special Invocation Analysis

    protected override CompilationUnitSyntax AnalyzeSpecialInvocations(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Analyze Record.Exception calls
        currentRoot = AnalyzeRecordExceptionCalls(currentRoot);

        // Analyze ITestOutputHelper.WriteLine calls
        currentRoot = AnalyzeTestOutputHelperCalls(currentRoot);

        return currentRoot;
    }

    private CompilationUnitSyntax AnalyzeRecordExceptionCalls(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Find Record.Exception calls on the ORIGINAL tree
        var recordExceptionCalls = OriginalRoot.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsRecordExceptionCall)
            .ToList();

        foreach (var originalCall in recordExceptionCalls)
        {
            try
            {
                // Check if it's assigned to a variable
                var parent = originalCall.Parent;
                if (parent is not EqualsValueClauseSyntax equalsClause ||
                    equalsClause.Parent is not VariableDeclaratorSyntax declarator)
                {
                    continue; // Only handle variable assignments
                }

                var variableName = declarator.Identifier.Text;

                // Extract the lambda body
                var lambda = originalCall.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                if (lambda == null) continue;

                string tryBlockBody;
                if (lambda is ParenthesizedLambdaExpressionSyntax parenLambda)
                {
                    tryBlockBody = ExtractLambdaBody(parenLambda.Body);
                }
                else if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    tryBlockBody = ExtractLambdaBody(simpleLambda.Body);
                }
                else
                {
                    continue;
                }

                var conversion = new RecordExceptionConversion
                {
                    VariableName = variableName,
                    TryBlockBody = tryBlockBody,
                    OriginalText = originalCall.ToString()
                };

                Plan.RecordExceptionConversions.Add(conversion);

                // Find the containing statement (the variable declaration statement)
                var declarationStatement = declarator.Ancestors()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .FirstOrDefault();

                if (declarationStatement != null)
                {
                    // Find corresponding node in current tree
                    var nodeToAnnotate = currentRoot.DescendantNodes()
                        .OfType<LocalDeclarationStatementSyntax>()
                        .FirstOrDefault(n => n.Span == declarationStatement.Span);

                    if (nodeToAnnotate != null)
                    {
                        var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(conversion.Annotation);
                        currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "RecordExceptionAnalysis",
                    Description = ex.Message,
                    OriginalCode = originalCall.ToString(),
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private bool IsRecordExceptionCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Record" } &&
            memberAccess.Name.Identifier.Text == "Exception")
        {
            // Semantic check to confirm it's xUnit
            var symbolInfo = SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                var containingType = methodSymbol.ContainingType?.ToDisplayString();
                return containingType?.StartsWith("Xunit.Record") == true;
            }
        }
        return false;
    }

    private static string ExtractLambdaBody(CSharpSyntaxNode body)
    {
        return body switch
        {
            BlockSyntax block => string.Join("\n", block.Statements.Select(s => s.ToString())),
            ExpressionSyntax expr => expr.ToString() + ";",
            _ => body.ToString()
        };
    }

    private CompilationUnitSyntax AnalyzeTestOutputHelperCalls(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Find ITestOutputHelper.WriteLine calls on the ORIGINAL tree
        var testOutputHelperCalls = OriginalRoot.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsTestOutputHelperWriteLineCall)
            .ToList();

        foreach (var originalCall in testOutputHelperCalls)
        {
            try
            {
                // Build the Console.WriteLine replacement
                var arguments = originalCall.ArgumentList.ToString();
                var replacementCode = $"Console.WriteLine{arguments}";

                var replacement = new InvocationReplacement
                {
                    ReplacementCode = replacementCode,
                    OriginalText = originalCall.ToString()
                };

                Plan.InvocationReplacements.Add(replacement);

                // Find corresponding node in current tree
                var nodeToAnnotate = currentRoot.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault(n => n.Span == originalCall.Span);

                if (nodeToAnnotate != null)
                {
                    var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(replacement.Annotation);
                    currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "TestOutputHelperAnalysis",
                    Description = ex.Message,
                    OriginalCode = originalCall.ToString(),
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private bool IsTestOutputHelperWriteLineCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "WriteLine")
        {
            // Check the receiver type semantically
            var receiverInfo = SemanticModel.GetTypeInfo(memberAccess.Expression);
            var typeName = receiverInfo.Type?.ToDisplayString();
            if (typeName == "Xunit.Abstractions.ITestOutputHelper")
            {
                return true;
            }

            // Fallback: syntactic check for common patterns
            // e.g., _testOutputHelper.WriteLine, TestOutputHelper.WriteLine, testOutputHelper.WriteLine
            var receiverName = memberAccess.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
                _ => null
            };

            if (receiverName != null &&
                (receiverName.Contains("testOutputHelper", StringComparison.OrdinalIgnoreCase) ||
                 receiverName.Contains("TestOutputHelper", StringComparison.OrdinalIgnoreCase) ||
                 receiverName.EndsWith("OutputHelper", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region TheoryData Analysis

    protected override CompilationUnitSyntax AnalyzeTheoryData(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Find all TheoryData<T> types in the original tree (field and property declarations)
        var theoryDataNodes = OriginalRoot.DescendantNodes()
            .OfType<GenericNameSyntax>()
            .Where(g => g.Identifier.Text == "TheoryData")
            .ToList();

        foreach (var originalGeneric in theoryDataNodes)
        {
            try
            {
                // Get the type argument
                var typeArg = originalGeneric.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg == null) continue;

                var elementType = typeArg.ToString();

                // Create annotations for both the type and the object creation
                var typeAnnotation = new SyntaxAnnotation("TUnitMigration", Guid.NewGuid().ToString());
                var creationAnnotation = new SyntaxAnnotation("TUnitMigration", Guid.NewGuid().ToString());

                var conversion = new TheoryDataConversion
                {
                    ElementType = elementType,
                    TypeAnnotation = typeAnnotation,
                    CreationAnnotation = creationAnnotation,
                    OriginalText = originalGeneric.ToString()
                };
                Plan.TheoryDataConversions.Add(conversion);

                // Find and annotate the GenericName in the current tree
                var typeToAnnotate = currentRoot.DescendantNodes()
                    .OfType<GenericNameSyntax>()
                    .FirstOrDefault(n => n.Span == originalGeneric.Span);

                if (typeToAnnotate != null)
                {
                    var annotatedType = typeToAnnotate.WithAdditionalAnnotations(typeAnnotation);
                    currentRoot = currentRoot.ReplaceNode(typeToAnnotate, annotatedType);
                }

                // Also find the corresponding object creation expression and annotate it
                // The object creation is typically a sibling or descendant node in the variable declaration
                var parent = originalGeneric.Parent;
                while (parent != null && parent is not VariableDeclarationSyntax)
                {
                    parent = parent.Parent;
                }

                if (parent is VariableDeclarationSyntax varDecl)
                {
                    foreach (var declarator in varDecl.Variables)
                    {
                        if (declarator.Initializer?.Value is BaseObjectCreationExpressionSyntax creation)
                        {
                            var creationToAnnotate = currentRoot.DescendantNodes()
                                .OfType<BaseObjectCreationExpressionSyntax>()
                                .FirstOrDefault(n => n.Span == creation.Span);

                            if (creationToAnnotate != null)
                            {
                                var annotatedCreation = creationToAnnotate.WithAdditionalAnnotations(creationAnnotation);
                                currentRoot = currentRoot.ReplaceNode(creationToAnnotate, annotatedCreation);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "TheoryDataAnalysis",
                    Description = ex.Message,
                    OriginalCode = originalGeneric.ToString(),
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    #endregion
}
