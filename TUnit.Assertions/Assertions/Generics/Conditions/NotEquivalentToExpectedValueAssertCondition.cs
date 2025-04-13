using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Equality;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NotEquivalentToExpectedValueAssertCondition<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TActual,  
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TExpected>(TExpected expected, string? expectedExpression) : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    private readonly List<string> _ignoredMembers = [];

    public EquivalencyKind EquivalencyKind { get; set; } = EquivalencyKind.Full;
    
    protected override string GetExpectation()
    {
        var expectedMessage = typeof(TExpected).IsSimpleType() || typeof(IEnumerable).IsAssignableFrom(typeof(TExpected)) 
            ? Formatter.Format(expected)
            : expectedExpression;
        
        return $"to not be equivalent to {expectedMessage ?? "null"}";
    }

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, TExpected? expectedValue)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return AssertionResult.Fail("both values are null");
        }

        if (actualValue is null || ExpectedValue is null)
        {
            return AssertionResult.Passed;
        }

        if (actualValue is IEnumerable actualEnumerable && ExpectedValue is IEnumerable expectedEnumerable)
        {
            var collectionEquivalentToEqualityComparer = new CollectionEquivalentToEqualityComparer<object?>(
                new CompareOptions
                {
                    MembersToIgnore = [.._ignoredMembers],
                    EquivalencyKind = EquivalencyKind,
                });
            
            var castedActual = actualEnumerable.Cast<object?>().ToArray();

            return AssertionResult
                .FailIf(castedActual.SequenceEqual(expectedEnumerable.Cast<object?>(),
                        collectionEquivalentToEqualityComparer), GetFailureMessage());
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
                .FailIf(isEqual.Value, GetFailureMessage());
        }

        var failures = Compare.CheckEquivalent(actualValue, ExpectedValue, new CompareOptions
        {
            MembersToIgnore = [.._ignoredMembers],
            EquivalencyKind = EquivalencyKind
        }, null).ToList();

        if (failures.FirstOrDefault() is not null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Fail(GetFailureMessage());
    }

    private static string GetFailureMessage()
    {
        return "it is";
    }

    public void IgnoringMember(string fieldName)
    {
        _ignoredMembers.Add(fieldName);
    }
}