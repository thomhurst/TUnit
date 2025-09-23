using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class SingleItemAssertionBuilderWrapper<TActual, TInner> : AssertionBuilder<TActual> where TActual : IEnumerable<TInner>
{
    internal SingleItemAssertionBuilderWrapper(AssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public new TaskAwaiter<TInner> GetAwaiter()
    {
        return Process().GetAwaiter();
    }
    
    private async Task<TInner> Process()
    {
        var assertionData = await GetAssertionData();
        await ProcessAssertionsAsync(assertionData);
        
        if (assertionData.Result is IEnumerable<TInner> enumerable)
        {
            return enumerable.SingleOrDefault()!;
        }

        return default(TInner)!;
    }
}