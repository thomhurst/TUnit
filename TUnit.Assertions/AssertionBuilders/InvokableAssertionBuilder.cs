using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableAssertionBuilder<TActual, TAnd, TOr> : 
    AssertionBuilder<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    public TAnd And { get; }
    public TOr Or { get; }
    
    internal InvokableAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual> assertionBuilder) : base(assertionDataDelegate)
    {
        Assertions.AddRange(assertionBuilder.Assertions);

        And = TAnd.Create(assertionDataDelegate, this);
        Or = TOr.Create(assertionDataDelegate, this);
    }

    public async Task ProcessAssertionsAsync()
    {
        var assertionData = await AssertionDataDelegate();

        await using (Assert.Multiple())
        {
            foreach (var assertion in Assertions)
            {
                assertion.AssertAndThrow(assertionData);
            }
        }
    }

    public TaskAwaiter GetAwaiter() => ProcessAssertionsAsync().GetAwaiter();
}