using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions;

public class SatisfiesAssertCondition<TActual, TExpected>(Func<TActual, Task<TExpected>?> mapper,
    Func<IValueSource<TExpected?>, InvokableAssertionBuilder<TExpected?>> assertionBuilder, string assertionBuilderExpression) : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => $"to satisfy {assertionBuilderExpression}";

    protected override async Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            return AssertionResult.Fail("is null");
        }
        
        var innerItemTask = mapper(actualValue);

        var innerItem = innerItemTask == null ? default : await innerItemTask;
        
        var innerAssertionBuilder = new ValueAssertionBuilder<TExpected?>(innerItem, "");

        var assertion = assertionBuilder(innerAssertionBuilder);
        
        foreach (var baseAssertCondition in assertion.Assertions)
        {
            var result = await baseAssertCondition.Assert(innerItem, exception, "");

            if (!result.IsPassed)
            {
                return result;
            }
        }
        
        return AssertionResult.Passed;
    }
}