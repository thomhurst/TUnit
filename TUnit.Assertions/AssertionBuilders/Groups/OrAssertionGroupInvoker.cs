using System.Runtime.CompilerServices;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders.Groups;

public class OrAssertionGroupInvoker<TActual, TAssertionBuilder> 
    where TAssertionBuilder : AssertionBuilder<TActual>
{
    private readonly List<AssertionGroup<TActual, TAssertionBuilder>> _assertionGroups = [];

    public OrAssertionGroupInvoker(AssertionGroup<TActual, TAssertionBuilder> group1, AssertionGroup<TActual, TAssertionBuilder> group2)
    {
        _assertionGroups.Add(group1);
        _assertionGroups.Add(group2);
    }

    public OrAssertionGroupInvoker<TActual, TAssertionBuilder> Or(AssertionGroup<TActual, TAssertionBuilder> group)
    {
        _assertionGroups.Add(group);
        return this;
    }
    
    public TaskAwaiter<TActual?> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<TActual?> Process()
    {
        var exceptions = new List<AssertionException>();
        
        foreach (var assertionGroup in _assertionGroups)
        {
            try
            {
                return await assertionGroup;
            }
            catch (AssertionException e)
            {
                exceptions.Add(e);
            }
        }

        throw new OrAssertionException(exceptions);
    }
}