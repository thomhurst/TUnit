using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TUnit.Assertions.AssertionBuilders;

public class CastedAssertionBuilder<TActual, TCasted> : AssertionBuilder<TActual>
{
    private readonly AssertionBuilder<TActual> _innerBuilder;
    
    public CastedAssertionBuilder(AssertionBuilder<TActual> innerBuilder)
        : base(innerBuilder.Actual, innerBuilder.ActualExpression)
    {
        _innerBuilder = innerBuilder;
        
        // Copy assertions from the original builder
        foreach (var assertion in innerBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }
    
    public new TaskAwaiter<TCasted?> GetAwaiter()
    {
        return CastResultAsync().GetAwaiter();
    }
    
    private async Task<TCasted?> CastResultAsync()
    {
        var data = await GetAssertionData();
        await ProcessAssertionsAsync(data);
        
        if (data.Result is TCasted casted)
        {
            return casted;
        }
        
        return default;
    }
}