using System.Collections;

namespace TUnit.Assertions.AssertConditions.Generic;

public class EquivalentToAssertCondition<TActual> : AssertCondition<TActual, TActual>
{
    public EquivalentToAssertCondition(TActual expected) : base(expected)
    {
    }

    private readonly List<string> _ignoredMembers = [];

    protected internal override string GetFailureMessage() => $"""
                                                               The two items were not equivalent
                                                                  Actual: {ActualValue}
                                                                  Expected: {ExpectedValue}
                                                               """;

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return true;
        }

        if (actualValue is null || ExpectedValue is null)
        {
            return false;
        }

        if (actualValue is IEqualityComparer<TActual> typedEqualityComparer)
        {
            return typedEqualityComparer.Equals(actualValue, ExpectedValue);
        }

        if (actualValue is IEqualityComparer basicEqualityComparer)
        {
            return basicEqualityComparer.Equals(actualValue, ExpectedValue);
        }

        if (ExpectedValue is IEqualityComparer<TActual> expectedTypeEqualityComparer)
        {
            return expectedTypeEqualityComparer.Equals(actualValue, ExpectedValue);
        }

        if (ExpectedValue is IEqualityComparer expectedBasicEqualityComparer)
        {
            return expectedBasicEqualityComparer.Equals(actualValue, ExpectedValue);
        }

        if (actualValue is IEnumerable enumerable && ExpectedValue is IEnumerable enumerable2)
        {
            return enumerable.Cast<object>().SequenceEqual(enumerable2.Cast<object>());
        }

        var failures = Compare.CheckEquivalent(actualValue, ExpectedValue, new CompareOptions()
        {
            MembersToIgnore = [.._ignoredMembers],
        }).ToList();

        if (failures.FirstOrDefault() is { } firstFailure)
        {
            if (firstFailure.Type == MemberType.Value)
            {
                return FailWithMessage($"""
                                        Expected: {Format(firstFailure.Expected)}
                                        Received: {Format(firstFailure.Actual)}
                                        """);
            }

            return FailWithMessage($"""
                                    {firstFailure.Type} {string.Join('.', firstFailure.NestedMemberNames)} did not match
                                    Expected: {Format(firstFailure.Expected)}
                                    Received: {Format(firstFailure.Actual)}
                                    """);
        }

        return true;
    }

    public void IgnoringMember(string fieldName)
    {
        _ignoredMembers.Add(fieldName);
    }
}