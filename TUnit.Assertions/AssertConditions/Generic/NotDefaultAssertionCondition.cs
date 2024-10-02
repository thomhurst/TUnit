namespace TUnit.Assertions.AssertConditions.Generic;

public class NotDefaultAssertCondition<TActual>() : AssertCondition<TActual, TActual>(default)
{
		private readonly TActual? _defaultValue = default;

	  protected internal override string GetFailureMessage()
				=> _defaultValue is null ? 
					     $"{ActualExpression ?? typeof(TActual).Name} was default value null" : 
					     $"{ActualExpression ?? typeof(TActual).Name} was default value {_defaultValue}";
	
	  protected override bool Passes(TActual? actualValue, Exception? exception)
				=> actualValue is not null && !actualValue.Equals(_defaultValue);
}