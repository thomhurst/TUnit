using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class EquivalentToExpectedValueAssertCondition<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual>(TActual expected, string expectedExpression) : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
    private readonly List<string> _ignoredMembers = [];

    protected override string GetExpectation()
        => $"to be equivalent to {expectedExpression}";

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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
                return FailWithMessage(Formatter.Format(firstFailure.Actual));
            }

            return FailWithMessage($"""
                                    {firstFailure.Type} {string.Join('.', firstFailure.NestedMemberNames)} did not match
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