using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class OrAssertionBuilder<TActual, TAnd, TOr> : InvokableAssertionBuilder<TActual, TAnd, TOr>,
    IOutputsChain<OrAssertionBuilder<TActual, TAnd, TOr>, TActual, TAnd, TOr> 
    where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    public TOr Or { get; }
    
    internal OrAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) : base(assertionDataDelegate, ChainType.Or)
    {
        Assertions.AddRange(assertionBuilder.Assertions);
        
        Or = TOr.Create(assertionDataDelegate, this);
    }
    
    private protected override async Task ProcessAssertionsAsync()
    {
        var assertionData = await AssertionDataDelegate();

        foreach (var assertion in Assertions)
        {
            if (assertion.Assert(assertionData))
            {
                return;
            }
        }

        await using (Assert.Multiple())
        {
            foreach (var assertion in Assertions)
            {
                assertion.AssertAndThrow(assertionData);
            }
        }
    }

    public static OrAssertionBuilder<TActual, TAnd, TOr> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
    {
        return new OrAssertionBuilder<TActual, TAnd, TOr>(assertionDataDelegate, assertionBuilder);
    }
}