using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Groups;

public class AndAssertionGroupInvoker<TActual, TAssertionBuilder> 
    where TAssertionBuilder : AssertionBuilder<TActual>
{
    private readonly List<AssertionGroup<TActual, TAssertionBuilder>> _assertionGroups = [];

    public AndAssertionGroupInvoker(AssertionGroup<TActual, TAssertionBuilder> group1, AssertionGroup<TActual, TAssertionBuilder> group2)
    {
        _assertionGroups.Add(group1);
        _assertionGroups.Add(group2);
    }

    public AndAssertionGroupInvoker<TActual, TAssertionBuilder> And(AssertionGroup<TActual, TAssertionBuilder> group)
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
        for (var i = 0; i < _assertionGroups.Count; i++)
        {
            var result = await _assertionGroups[i];
            
            if (i == _assertionGroups.Count - 1)
            {
                return result;
            }
        }

        return default;
    }
}