using System.Collections;

namespace TUnit.Assertions.AssertConditions.Generic;

public class EquivalentToExpectedValueAssertCondition<TActual>(TActual expected, string expectedExpression) : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
    private readonly List<string> _ignoredMembers = [];

    protected override string GetExpectation()
        => $"to be equivalent to {expectedExpression}";

    protected override AssertionResult GetResult(TActual? actualValue, TActual? expectedValue)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return AssertionResult.Passed;
        }

        if (actualValue is null || ExpectedValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => actualValue is null,
                    "it is null")
                .OrFailIf(
                    () => expectedValue is null,
                    "it is not null");
        }

        bool? isEqual = null;
        if (actualValue is IEqualityComparer<TActual> typedEqualityComparer)
        {
            isEqual = typedEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        else if (actualValue is IEqualityComparer basicEqualityComparer)
        {
            isEqual = basicEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        else if (ExpectedValue is IEqualityComparer<TActual> expectedTypeEqualityComparer)
        {
            isEqual = expectedTypeEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        else if (ExpectedValue is IEqualityComparer expectedBasicEqualityComparer)
        {
            isEqual = expectedBasicEqualityComparer.Equals(actualValue, ExpectedValue);
        }
        else if (actualValue is IEnumerable enumerable && ExpectedValue is IEnumerable enumerable2)
        {
            isEqual = enumerable.Cast<object>().SequenceEqual(enumerable2.Cast<object>());
        }
        if (isEqual != null)
        {
            return AssertionResult
                .FailIf(
                    () => !isEqual.Value,
                    $"found {actualValue}");
        }

        var failures = Compare.CheckEquivalent(actualValue, ExpectedValue, new CompareOptions
        {
            MembersToIgnore = [.._ignoredMembers],
        }).ToList();

        if (failures.FirstOrDefault() is { } firstFailure)
        {
            if (firstFailure.Type == MemberType.Value)
            {
                return FailWithMessage(Format(firstFailure.Actual));
            }

            return FailWithMessage($"""
                                    {firstFailure.Type} {string.Join('.', firstFailure.NestedMemberNames)} did not match
                                    Expected: {Format(firstFailure.Expected)}
                                    Received: {Format(firstFailure.Actual)}
                                    """);
        }

        return AssertionResult.Passed;
    }

    public void IgnoringMember(string fieldName)
    {
        _ignoredMembers.Add(fieldName);
    }
}