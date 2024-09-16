using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class AndAssertionBuilder<TActual, TAnd, TOr> : InvokableAssertionBuilder<TActual, TAnd, TOr>,
    IOutputsChain<AndAssertionBuilder<TActual, TAnd, TOr>, TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    public TAnd And { get; }
    
    internal AndAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) : base(assertionDataDelegate, ChainType.And)
    {
        Assertions.AddRange(assertionBuilder.Assertions);
        
        And = TAnd.Create(assertionDataDelegate, this);
    }
    
    private protected override async Task ProcessAssertionsAsync()
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
    
    public static AndAssertionBuilder<TActual, TAnd, TOr> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
    {
        return new AndAssertionBuilder<TActual, TAnd, TOr>(assertionDataDelegate, assertionBuilder);
    }
}