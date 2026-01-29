using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base.TwoPhase;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers.CodeFixers.TwoPhase;

/// <summary>
/// Phase 1 analyzer for MSTest to TUnit migration.
/// Collects all conversion targets while the semantic model is valid.
/// </summary>
public class MSTestTwoPhaseAnalyzer : MigrationAnalyzer
{
    private static readonly HashSet<string> MSTestAssertMethods = new()
    {
        "AreEqual", "AreNotEqual", "AreSame", "AreNotSame",
        "IsTrue", "IsFalse",
        "IsNull", "IsNotNull",
        "IsInstanceOfType", "IsNotInstanceOfType",
        "ThrowsException", "ThrowsExceptionAsync",
        "Fail", "Inconclusive"
    };

    private static readonly HashSet<string> MSTestCollectionAssertMethods = new()
    {
        "AreEqual", "AreNotEqual", "AreEquivalent", "AreNotEquivalent",
        "Contains", "DoesNotContain",
        "IsSubsetOf", "IsNotSubsetOf",
        "AllItemsAreUnique", "AllItemsAreNotNull", "AllItemsAreInstancesOfType"
    };

    private static readonly HashSet<string> MSTestStringAssertMethods = new()
    {
        "Contains", "StartsWith", "EndsWith", "Matches", "DoesNotMatch"
    };

    private static readonly HashSet<string> MSTestFileAssertMethods = new()
    {
        "Exists", "DoesNotExist"
    };

    private static readonly HashSet<string> MSTestDirectoryAssertMethods = new()
    {
        "Exists", "DoesNotExist"
    };

    private static readonly HashSet<string> MSTestAttributeNames = new()
    {
        "TestClass", "TestMethod", "DataRow", "DynamicData",
        "TestInitialize", "TestCleanup", "ClassInitialize", "ClassCleanup",
        "TestCategory", "Ignore", "Priority", "Owner", "ExpectedException"
    };

    private static readonly HashSet<string> MSTestRemovableAttributeNames = new()
    {
        "TestClass" // TestClass is implicit in TUnit
    };

    public MSTestTwoPhaseAnalyzer(SemanticModel semanticModel, Compilation compilation)
        : base(semanticModel, compilation)
    {
    }

    protected override IEnumerable<InvocationExpressionSyntax> FindAssertionNodes(CompilationUnitSyntax root)
    {
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsMSTestAssertion);
    }

    private bool IsMSTestAssertion(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression is IdentifierNameSyntax identifier)
            {
                var typeName = identifier.Identifier.Text;
                var methodName = memberAccess.Name.Identifier.Text;

                // Check Assert methods
                if (typeName == "Assert" && MSTestAssertMethods.Contains(methodName))
                {
                    return VerifyMSTestNamespace(invocation);
                }

                // Check CollectionAssert methods
                if (typeName == "CollectionAssert" && MSTestCollectionAssertMethods.Contains(methodName))
                {
                    return VerifyMSTestNamespace(invocation);
                }

                // Check StringAssert methods
                if (typeName == "StringAssert" && MSTestStringAssertMethods.Contains(methodName))
                {
                    return VerifyMSTestNamespace(invocation);
                }

                // Check FileAssert methods
                if (typeName == "FileAssert" && MSTestFileAssertMethods.Contains(methodName))
                {
                    return VerifyMSTestNamespace(invocation);
                }

                // Check DirectoryAssert methods
                if (typeName == "DirectoryAssert" && MSTestDirectoryAssertMethods.Contains(methodName))
                {
                    return VerifyMSTestNamespace(invocation);
                }
            }
        }

        return false;
    }

    private bool VerifyMSTestNamespace(InvocationExpressionSyntax invocation)
    {
        try
        {
            var symbolInfo = SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                var containingNamespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
                // Only return false if we positively know it's NOT MSTest
                if (containingNamespace != null && !containingNamespace.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting"))
                {
                    return false;
                }
                // Namespace matches or is null (resolution failed) - assume it's MSTest
                return true;
            }
        }
        catch
        {
            // Fall back to syntax-based detection
        }

        return true; // Assume it's MSTest if syntax matches
    }

    protected override AssertionConversion? AnalyzeAssertion(InvocationExpressionSyntax node)
    {
        if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        var typeName = (memberAccess.Expression as IdentifierNameSyntax)?.Identifier.Text ?? "";
        var methodName = memberAccess.Name.Identifier.Text;
        var arguments = node.ArgumentList.Arguments;

        var (kind, replacementCode, introducesAwait, todoComment) = typeName switch
        {
            "Assert" => ConvertAssertMethod(methodName, arguments, memberAccess),
            "CollectionAssert" => ConvertCollectionAssertMethod(methodName, arguments),
            "StringAssert" => ConvertStringAssertMethod(methodName, arguments),
            "FileAssert" => ConvertFileAssertMethod(methodName, arguments),
            "DirectoryAssert" => ConvertDirectoryAssertMethod(methodName, arguments),
            _ => (AssertionConversionKind.Unknown, null, false, null)
        };

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

    private (AssertionConversionKind, string?, bool, string?) ConvertAssertMethod(
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
            "IsInstanceOfType" => ConvertIsInstanceOfType(args),
            "IsNotInstanceOfType" => ConvertIsNotInstanceOfType(args),
            "ThrowsException" => ConvertThrowsException(memberAccess, args),
            "ThrowsExceptionAsync" => ConvertThrowsExceptionAsync(memberAccess, args),
            "Fail" => ConvertFail(args),
            "Inconclusive" => ConvertInconclusive(args),
            _ => (AssertionConversionKind.Unknown, null, false, null)
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertAreEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.AreEqual, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();

        // Check for message parameter (3rd or later)
        var (message, hasComparer) = GetMessageWithFormatArgs(args, 2);

        string? todoComment = hasComparer
            ? "// TODO: TUnit migration - IEqualityComparer was used. TUnit uses .IsEqualTo() which may have different comparison semantics."
            : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsEqualTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsEqualTo({expected})";

        return (AssertionConversionKind.AreEqual, assertion, true, todoComment);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertAreNotEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.AreNotEqual, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        var (message, hasComparer) = GetMessageWithFormatArgs(args, 2);

        string? todoComment = hasComparer
            ? "// TODO: TUnit migration - IEqualityComparer was used. TUnit uses .IsNotEqualTo() which may have different comparison semantics."
            : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsNotEqualTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsNotEqualTo({expected})";

        return (AssertionConversionKind.AreNotEqual, assertion, true, todoComment);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertAreSame(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.AreSame, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

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
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsNotSameReferenceAs({expected}).Because({message})"
            : $"await Assert.That({actual}).IsNotSameReferenceAs({expected})";

        return (AssertionConversionKind.AreNotSame, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsTrue(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsTrue, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args, 1) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsTrue().Because({message})"
            : $"await Assert.That({value}).IsTrue()";

        return (AssertionConversionKind.IsTrue, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsFalse(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsFalse, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args, 1) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsFalse().Because({message})"
            : $"await Assert.That({value}).IsFalse()";

        return (AssertionConversionKind.IsFalse, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsNull(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsNull, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args, 1) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsNull().Because({message})"
            : $"await Assert.That({value}).IsNull()";

        return (AssertionConversionKind.IsNull, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsNotNull(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.IsNotNull, null, false, null);

        var value = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args, 1) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsNotNull().Because({message})"
            : $"await Assert.That({value}).IsNotNull()";

        return (AssertionConversionKind.IsNotNull, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsInstanceOfType(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.IsInstanceOfType, null, false, null);

        var value = args[0].Expression.ToString();
        var expectedType = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsAssignableTo({expectedType}).Because({message})"
            : $"await Assert.That({value}).IsAssignableTo({expectedType})";

        return (AssertionConversionKind.IsInstanceOfType, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertIsNotInstanceOfType(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.IsNotInstanceOfType, null, false, null);

        var value = args[0].Expression.ToString();
        var expectedType = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).IsNotAssignableTo({expectedType}).Because({message})"
            : $"await Assert.That({value}).IsNotAssignableTo({expectedType})";

        return (AssertionConversionKind.IsNotInstanceOfType, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertThrowsException(
        MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.ThrowsException, null, false, null);

        var action = args[0].Expression.ToString();

        // Get generic type argument if present
        string typeArg = "Exception";
        if (memberAccess.Name is GenericNameSyntax genericName &&
            genericName.TypeArgumentList.Arguments.Count > 0)
        {
            typeArg = genericName.TypeArgumentList.Arguments[0].ToString();
        }

        var assertion = $"await Assert.ThrowsAsync<{typeArg}>({action})";
        return (AssertionConversionKind.ThrowsException, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertThrowsExceptionAsync(
        MemberAccessExpressionSyntax memberAccess, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        // Same conversion as ThrowsException
        return ConvertThrowsException(memberAccess, args);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertFail(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var message = args.Count > 0 ? args[0].Expression.ToString() : "\"\"";
        var assertion = $"await Assert.Fail({message})";
        return (AssertionConversionKind.Fail, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertInconclusive(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var message = args.Count > 0 ? args[0].Expression.ToString() : "\"Test inconclusive\"";
        var assertion = $"await Assert.Skip({message})";
        return (AssertionConversionKind.Inconclusive, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAssertMethod(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        return methodName switch
        {
            "AreEqual" => ConvertCollectionAreEqual(args),
            "AreNotEqual" => ConvertCollectionAreNotEqual(args),
            "AreEquivalent" => ConvertCollectionAreEquivalent(args),
            "AreNotEquivalent" => ConvertCollectionAreNotEquivalent(args),
            "Contains" => ConvertCollectionContains(args),
            "DoesNotContain" => ConvertCollectionDoesNotContain(args),
            "IsSubsetOf" => ConvertCollectionIsSubsetOf(args),
            "IsNotSubsetOf" => ConvertCollectionIsNotSubsetOf(args),
            "AllItemsAreUnique" => ConvertCollectionAllItemsAreUnique(args),
            "AllItemsAreNotNull" => ConvertCollectionAllItemsAreNotNull(args),
            "AllItemsAreInstancesOfType" => ConvertCollectionAllItemsAreInstancesOfType(args),
            _ => (AssertionConversionKind.Unknown, null, false, null)
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAreEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionAreEqual, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsEquivalentTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsEquivalentTo({expected})";

        return (AssertionConversionKind.CollectionAreEqual, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAreNotEqual(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionAreNotEqual, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsNotEquivalentTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsNotEquivalentTo({expected})";

        return (AssertionConversionKind.CollectionAreNotEqual, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAreEquivalent(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionAreEquivalent, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsEquivalentTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsEquivalentTo({expected})";

        return (AssertionConversionKind.CollectionAreEquivalent, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAreNotEquivalent(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionAreNotEquivalent, null, false, null);

        var expected = args[0].Expression.ToString();
        var actual = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({actual}).IsNotEquivalentTo({expected}).Because({message})"
            : $"await Assert.That({actual}).IsNotEquivalentTo({expected})";

        return (AssertionConversionKind.CollectionAreNotEquivalent, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionContains(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionContains, null, false, null);

        var collection = args[0].Expression.ToString();
        var element = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).Contains({element}).Because({message})"
            : $"await Assert.That({collection}).Contains({element})";

        return (AssertionConversionKind.CollectionContains, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionDoesNotContain(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionDoesNotContain, null, false, null);

        var collection = args[0].Expression.ToString();
        var element = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).DoesNotContain({element}).Because({message})"
            : $"await Assert.That({collection}).DoesNotContain({element})";

        return (AssertionConversionKind.CollectionDoesNotContain, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionIsSubsetOf(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionIsSubsetOf, null, false, null);

        var subset = args[0].Expression.ToString();
        var superset = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({subset}).IsSubsetOf({superset}).Because({message})"
            : $"await Assert.That({subset}).IsSubsetOf({superset})";

        return (AssertionConversionKind.CollectionIsSubsetOf, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionIsNotSubsetOf(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionIsNotSubsetOf, null, false, null);

        var subset = args[0].Expression.ToString();
        var superset = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({subset}).IsNotSubsetOf({superset}).Because({message})"
            : $"await Assert.That({subset}).IsNotSubsetOf({superset})";

        return (AssertionConversionKind.CollectionIsNotSubsetOf, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAllItemsAreUnique(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.CollectionAllItemsAreUnique, null, false, null);

        var collection = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args, 1) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).HasDistinctItems().Because({message})"
            : $"await Assert.That({collection}).HasDistinctItems()";

        return (AssertionConversionKind.CollectionAllItemsAreUnique, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAllItemsAreNotNull(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1) return (AssertionConversionKind.CollectionAllItemsAreNotNull, null, false, null);

        var collection = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args, 1) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).All(x => x != null).Because({message})"
            : $"await Assert.That({collection}).All(x => x != null)";

        return (AssertionConversionKind.CollectionAllItemsAreNotNull, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertCollectionAllItemsAreInstancesOfType(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.CollectionAllItemsAreInstancesOfType, null, false, null);

        var collection = args[0].Expression.ToString();
        var expectedType = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({collection}).All(x => {expectedType}.IsInstanceOfType(x)).Because({message})"
            : $"await Assert.That({collection}).All(x => {expectedType}.IsInstanceOfType(x))";

        return (AssertionConversionKind.CollectionAllItemsAreInstancesOfType, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringAssertMethod(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        return methodName switch
        {
            "Contains" => ConvertStringContains(args),
            "StartsWith" => ConvertStringStartsWith(args),
            "EndsWith" => ConvertStringEndsWith(args),
            "Matches" => ConvertStringMatches(args),
            "DoesNotMatch" => ConvertStringDoesNotMatch(args),
            _ => (AssertionConversionKind.Unknown, null, false, null)
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringContains(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.StringContains, null, false, null);

        var value = args[0].Expression.ToString();
        var substring = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).Contains({substring}).Because({message})"
            : $"await Assert.That({value}).Contains({substring})";

        return (AssertionConversionKind.StringContains, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringStartsWith(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.StringStartsWith, null, false, null);

        var value = args[0].Expression.ToString();
        var prefix = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).StartsWith({prefix}).Because({message})"
            : $"await Assert.That({value}).StartsWith({prefix})";

        return (AssertionConversionKind.StringStartsWith, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringEndsWith(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.StringEndsWith, null, false, null);

        var value = args[0].Expression.ToString();
        var suffix = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).EndsWith({suffix}).Because({message})"
            : $"await Assert.That({value}).EndsWith({suffix})";

        return (AssertionConversionKind.StringEndsWith, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringMatches(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.StringMatches, null, false, null);

        var value = args[0].Expression.ToString();
        var pattern = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).Matches({pattern}).Because({message})"
            : $"await Assert.That({value}).Matches({pattern})";

        return (AssertionConversionKind.StringMatches, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertStringDoesNotMatch(SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 2) return (AssertionConversionKind.StringDoesNotMatch, null, false, null);

        var value = args[0].Expression.ToString();
        var pattern = args[1].Expression.ToString();
        string? message = args.Count >= 3 ? GetMessageArgument(args, 2) : null;

        var assertion = message != null
            ? $"await Assert.That({value}).DoesNotMatch({pattern}).Because({message})"
            : $"await Assert.That({value}).DoesNotMatch({pattern})";

        return (AssertionConversionKind.StringDoesNotMatch, assertion, true, null);
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertFileAssertMethod(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1)
            return (AssertionConversionKind.Unknown, null, false, null);

        var fileArg = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args, 1) : null;

        // MSTest FileAssert methods take a FileInfo argument
        // FileAssert.Exists(file) -> Assert.That(file.Exists).IsTrue()
        // FileAssert.DoesNotExist(file) -> Assert.That(file.Exists).IsFalse()
        return methodName switch
        {
            "Exists" => message != null
                ? (AssertionConversionKind.True, $"await Assert.That({fileArg}.Exists).IsTrue().Because({message})", true, null)
                : (AssertionConversionKind.True, $"await Assert.That({fileArg}.Exists).IsTrue()", true, null),
            "DoesNotExist" => message != null
                ? (AssertionConversionKind.False, $"await Assert.That({fileArg}.Exists).IsFalse().Because({message})", true, null)
                : (AssertionConversionKind.False, $"await Assert.That({fileArg}.Exists).IsFalse()", true, null),
            _ => (AssertionConversionKind.Unknown, null, false, $"// TODO: TUnit migration - FileAssert.{methodName} has no direct equivalent")
        };
    }

    private (AssertionConversionKind, string?, bool, string?) ConvertDirectoryAssertMethod(
        string methodName, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (args.Count < 1)
            return (AssertionConversionKind.Unknown, null, false, null);

        var dirArg = args[0].Expression.ToString();
        string? message = args.Count >= 2 ? GetMessageArgument(args, 1) : null;

        // MSTest DirectoryAssert methods take a DirectoryInfo argument
        // DirectoryAssert.Exists(dir) -> Assert.That(dir.Exists).IsTrue()
        // DirectoryAssert.DoesNotExist(dir) -> Assert.That(dir.Exists).IsFalse()
        return methodName switch
        {
            "Exists" => message != null
                ? (AssertionConversionKind.True, $"await Assert.That({dirArg}.Exists).IsTrue().Because({message})", true, null)
                : (AssertionConversionKind.True, $"await Assert.That({dirArg}.Exists).IsTrue()", true, null),
            "DoesNotExist" => message != null
                ? (AssertionConversionKind.False, $"await Assert.That({dirArg}.Exists).IsFalse().Because({message})", true, null)
                : (AssertionConversionKind.False, $"await Assert.That({dirArg}.Exists).IsFalse()", true, null),
            _ => (AssertionConversionKind.Unknown, null, false, $"// TODO: TUnit migration - DirectoryAssert.{methodName} has no direct equivalent")
        };
    }

    /// <summary>
    /// Gets the message argument, handling format strings and detecting comparer usage.
    /// Returns (message, hasComparer) where message is wrapped in string.Format if format args present.
    /// </summary>
    private static (string? message, bool hasComparer) GetMessageWithFormatArgs(SeparatedSyntaxList<ArgumentSyntax> args, int startIndex)
    {
        if (args.Count <= startIndex)
            return (null, false);

        var arg = args[startIndex];

        // Check if it's named "message"
        if (arg.NameColon?.Name.Identifier.Text == "message")
        {
            return (arg.Expression.ToString(), false);
        }

        // Check if it's a string literal (potential format string or simple message)
        if (arg.Expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            var messageString = arg.Expression.ToString();

            // Check if there are format arguments after the message (4+ total args means format string)
            if (args.Count > startIndex + 1)
            {
                // Collect format args
                var formatArgs = new List<string>();
                for (int i = startIndex + 1; i < args.Count; i++)
                {
                    formatArgs.Add(args[i].Expression.ToString());
                }
                // Wrap in string.Format
                return ($"string.Format({messageString}, {string.Join(", ", formatArgs)})", false);
            }

            return (messageString, false);
        }

        // Check for interpolated string
        if (arg.Expression is InterpolatedStringExpressionSyntax)
        {
            return (arg.Expression.ToString(), false);
        }

        // Not a string - likely a comparer
        return (null, true);
    }

    /// <summary>
    /// Gets a simple message argument (for methods that don't support format strings like CollectionAssert).
    /// </summary>
    private static string? GetMessageArgument(SeparatedSyntaxList<ArgumentSyntax> args, int startIndex)
    {
        if (args.Count > startIndex)
        {
            var arg = args[startIndex];
            // Check if it's named "message"
            if (arg.NameColon?.Name.Identifier.Text == "message")
            {
                return arg.Expression.ToString();
            }
            // Check if it looks like a string
            if (arg.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return arg.Expression.ToString();
            }
            if (arg.Expression is InterpolatedStringExpressionSyntax)
            {
                return arg.Expression.ToString();
            }
        }
        return null;
    }

    protected override bool ShouldRemoveAttribute(AttributeSyntax node)
    {
        var name = MigrationHelpers.GetAttributeName(node);
        return MSTestRemovableAttributeNames.Contains(name);
    }

    protected override AttributeConversion? AnalyzeAttribute(AttributeSyntax node)
    {
        var name = MigrationHelpers.GetAttributeName(node);

        if (!MSTestAttributeNames.Contains(name))
            return null;

        var (newName, newArgs) = name switch
        {
            "TestMethod" => ("Test", null),
            "DataRow" => ("Arguments", node.ArgumentList?.ToString()),
            "DynamicData" => ("MethodDataSource", ConvertDynamicDataArgs(node)),
            "TestInitialize" => ("Before", "(HookType.Test)"),
            "TestCleanup" => ("After", "(HookType.Test)"),
            "ClassInitialize" => ("Before", "(HookType.Class)"),
            "ClassCleanup" => ("After", "(HookType.Class)"),
            "TestCategory" => ("Property", ConvertTestCategoryArgs(node)),
            "Ignore" => ("Skip", node.ArgumentList?.ToString()),
            "Priority" => ("Property", ConvertPriorityArgs(node)),
            "Owner" => ("Property", ConvertOwnerArgs(node)),
            "ExpectedException" => (null, null), // Handled separately
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

    private static string? ConvertDynamicDataArgs(AttributeSyntax node)
    {
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = node.ArgumentList.Arguments[0];
            return $"({firstArg.Expression})";
        }
        return null;
    }

    private static string? ConvertTestCategoryArgs(AttributeSyntax node)
    {
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var value = node.ArgumentList.Arguments[0].Expression.ToString();
            return $"(\"Category\", {value})";
        }
        return null;
    }

    private static string? ConvertPriorityArgs(AttributeSyntax node)
    {
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var value = node.ArgumentList.Arguments[0].Expression.ToString();
            return $"(\"Priority\", \"{value}\")";
        }
        return null;
    }

    private static string? ConvertOwnerArgs(AttributeSyntax node)
    {
        if (node.ArgumentList?.Arguments.Count > 0)
        {
            var value = node.ArgumentList.Arguments[0].Expression.ToString();
            return $"(\"Owner\", {value})";
        }
        return null;
    }

    protected override bool ShouldRemoveBaseType(BaseTypeSyntax baseType)
    {
        // MSTest doesn't have common base types to remove
        return false;
    }

    protected override void AnalyzeUsings()
    {
        Plan.UsingPrefixesToRemove.Add("Microsoft.VisualStudio.TestTools");
        // TUnit usings are handled automatically by MigrationHelpers
    }
}
