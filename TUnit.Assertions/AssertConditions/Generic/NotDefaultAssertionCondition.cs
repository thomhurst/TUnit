namespace TUnit.Assertions.AssertConditions.Generic;

public class NotDefaultExpectedValueAssertCondition<TActual>() : ExpectedValueAssertCondition<TActual, TActual>(default)
{
		private readonly TActual? _defaultValue = default;

	  protected override string GetFailureMessage(TActual? actualValue, TActual? expectedValue)
				=> _defaultValue is null ? 
					     $"{ActualExpression ?? typeof(TActual).Name} was default value null" : 
					     $"{ActualExpression ?? typeof(TActual).Name} was default value {_defaultValue}";
	
	  protected override AssertionResult Passes(TActual? actualValue, TActual? expectedValue)
				=> actualValue is not null && !actualValue.Equals(_defaultValue);
}