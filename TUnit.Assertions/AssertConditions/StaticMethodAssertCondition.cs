using System;
using System.Threading.Tasks;

namespace TUnit.Assertions.AssertConditions;

public class StaticMethodAssertCondition<T> : BaseAssertCondition<T>
{
    private readonly Func<T, bool> _predicate;
    private readonly string _methodName;
    private readonly bool _negated;
    
    public StaticMethodAssertCondition(Func<T, bool> predicate, string methodName, bool negated = false)
    {
        _predicate = predicate;
        _methodName = methodName;
        _negated = negated;
    }
    
    protected override ValueTask<AssertionResult> GetResult(T? actualValue, Exception? exception, AssertionMetadata assertionMetadata)
    {
        if (actualValue is null)
        {
            return AssertionResult.Fail("Actual value is null");
        }
        
        var result = _predicate(actualValue);
        var condition = _negated ? result : !result;
        var expectationVerb = _negated ? "not to satisfy" : "to satisfy";
        
        return AssertionResult.FailIf(condition, 
            $"'{actualValue}' was expected {expectationVerb} {_methodName}()");
    }
    
    internal protected override string GetExpectation()
    {
        var expectationVerb = _negated ? "not to satisfy" : "to satisfy";
        return $"{expectationVerb} {_methodName}()";
    }
}