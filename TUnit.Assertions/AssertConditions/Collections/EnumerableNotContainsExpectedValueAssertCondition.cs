namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotContainsExpectedValueAssertCondition<TActual, TInner>(
    TInner expected,
    IEqualityComparer<TInner?>? equalityComparer)
    : ExpectedValueAssertCondition<TActual, TInner>(expected)
    where TActual : IEnumerable<TInner>
{
    protected internal override string GetFailureMessage() => $"to not contain {expected}";

    protected internal override AssertionResult Passes(TActual? actualValue, TInner? inner)
		=> AssertionResult
			.FailIf(
				() => actualValue is null,
				$"{ActualExpression ?? typeof(TActual).Name} is null")
			.OrFailIf(
				() => actualValue.Contains(inner, equalityComparer),
				"it was found in the collection");
}