namespace TUnit.Assertions.AssertConditions.Generic;

public class DefaultExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
	  private readonly TActual? _defaultValue = default;
	
		protected internal override string GetFailureMessage()
			  => _defaultValue is null ?
						   $"{ActualExpression ?? typeof(TActual).Name} was not default value null" : 
						   $"{ActualExpression ?? typeof(TActual).Name} was not default value {_defaultValue}";

		protected override AssertionResult Passes(TActual? actualValue, Exception? exception)
				=> actualValue is null || actualValue.Equals(_defaultValue);
} 