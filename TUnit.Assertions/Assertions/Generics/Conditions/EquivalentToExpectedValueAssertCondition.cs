using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Equality;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class EquivalentToExpectedValueAssertCondition<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TExpected>(TExpected expected, string expectedExpression) : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    private readonly List<string> _ignoredMembers = [];

    protected override string GetExpectation()
        => $"to be equivalent to {expectedExpression}";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, TExpected? expectedValue)
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

        if (actualValue is IEnumerable actualEnumerable && ExpectedValue is IEnumerable expectedEnumerable)
        {
            var collectionEquivalentToEqualityComparer = new CollectionEquivalentToEqualityComparer<object?>(
                new CompareOptions
                {
                    MembersToIgnore = [.._ignoredMembers]
                });
            
            var castedActual = actualEnumerable.Cast<object?>().ToArray();

            return AssertionResult
                .FailIf(!castedActual.SequenceEqual(expectedEnumerable.Cast<object?>(),
                        collectionEquivalentToEqualityComparer),
                    $"{GetFailureMessage(castedActual, collectionEquivalentToEqualityComparer)}");
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
        else if (actualValue is IEnumerable enumerable && ExpectedValue is IEnumerable enumerable2)
        {
            IEnumerable<object> castedEnumerable = [..enumerable];
            IEnumerable<object> castedEnumerable2 = [..enumerable2];
            
            isEqual = castedEnumerable.SequenceEqual(castedEnumerable2);
        }
        if (isEqual != null)
        {
            return AssertionResult
                .FailIf(!isEqual.Value,
                    $"found {actualValue}");
        }

        var failures = Compare.CheckEquivalent(actualValue, ExpectedValue, new CompareOptions
        {
            MembersToIgnore = [.._ignoredMembers],
        }, null).ToList();

        if (failures.FirstOrDefault() is { } firstFailure)
        {
            if (firstFailure.Type == MemberType.Value)
            {
                return FailWithMessage(Formatter.Format(firstFailure.Actual));
            }

            return FailWithMessage($"""
                                    {firstFailure.Type} {string.Join(".", firstFailure.NestedMemberNames)} did not match
                                    Expected: {Formatter.Format(firstFailure.Expected)}
                                    Received: {Formatter.Format(firstFailure.Actual)}
                                    """);
        }

        return AssertionResult.Passed;
    }

    private static string GetFailureMessage(object?[] castedActual,
        CollectionEquivalentToEqualityComparer<object?> collectionEquivalentToEqualityComparer)
    {
        if (castedActual.ElementAtOrDefault(0)?.GetType().IsSimpleType() == false)
        {
            return collectionEquivalentToEqualityComparer.GetFailureMessages();
        }

        return $"it is {Formatter.Format(castedActual)}";
    }

    public void IgnoringMember(string fieldName)
    {
        _ignoredMembers.Add(fieldName);
    }
}