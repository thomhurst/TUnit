using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class NoneAssertionBuilder<TActual, TAnd, TOr> : InvokableAssertionBuilder<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    public TAnd And { get; }
    public TOr Or { get; }
    
    internal NoneAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) : base(assertionDataDelegate)
    {
        Assertions.AddRange(assertionBuilder.Assertions);

        And = TAnd.Create(assertionDataDelegate, this);
        Or = TOr.Create(assertionDataDelegate, this);
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
}