using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class EquivalentToExpectedValueAssertCondition<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
TActual,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
TExpected>(TExpected expected, string? expectedExpression) : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    private readonly List<string> _ignoredMembers = [];

    public EquivalencyKind EquivalencyKind { get; set; } = EquivalencyKind.Full;

    internal protected override string GetExpectation()
    {
        var expectedMessage = typeof(TExpected).IsSimpleType() || typeof(IEnumerable).IsAssignableFrom(typeof(TExpected))
            ? Formatter.Format(ExpectedValue)
            : expectedExpression;

        return $"to be equivalent to {expectedMessage ?? "null"}";
    }

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, TExpected? expectedValue)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return AssertionResult.Passed;
        }

        if (actualValue is null || ExpectedValue is null)
        {
            return AssertionResult
                .FailIf(actualValue is null,
                    "it is null")
                .OrFailIf(expectedValue is null,
                    "it is not null");
        }

        bool? isEqual = null;

        if (actualValue is IEqualityComparer basicEqualityComparer)
        {
            isEqual = basicEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        else if (ExpectedValue is IEqualityComparer expectedBasicEqualityComparer)
        {
            isEqual = expectedBasicEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        if (isEqual != null)
        {
            return AssertionResult
                .FailIf(!isEqual.Value,
                    $"found {actualValue}");
        }

        var failures = Compare.CheckEquivalent(actualValue, ExpectedValue, new CompareOptions
        {
            MembersToIgnore = [.. _ignoredMembers],
            EquivalencyKind = EquivalencyKind
        }, null).ToList();

        if (failures.FirstOrDefault() is { } firstFailure)
        {
            if (firstFailure.Type == MemberType.Value)
            {
                return FailWithMessage(Formatter.Format(firstFailure.Actual));
            }

            if (firstFailure.Type == MemberType.EnumerableItem)
            {
                return FailWithMessage($"""
                                        {string.Join(".", firstFailure.NestedMemberNames)} did not match
                                        Expected: {Formatter.Format(firstFailure.Expected)}
                                        Received: {Formatter.Format(firstFailure.Actual)}
                                        """);
            }

            return FailWithMessage($"""
                                    {firstFailure.Type} {string.Join(".", firstFailure.NestedMemberNames)} did not match
                                    Expected: {Formatter.Format(firstFailure.Expected)}
                                    Received: {Formatter.Format(firstFailure.Actual)}
                                    """);
        }

        return AssertionResult.Passed;
    }

    public void IgnoringMember(string fieldName)
    {
        _ignoredMembers.Add(fieldName);
    }
}
