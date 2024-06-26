namespace TUnit.Engine.Helpers;

public class CombinativeSolver
{
    private static readonly IEnumerable<IEnumerable<object?>> Seed = new[] { Enumerable.Empty<object?>() };
    
    public IEnumerable<IEnumerable<object?>> GetCombinativeArgumentsList(IEnumerable<IReadOnlyList<object?>> elements)
    {
        return elements.Aggregate(Seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }
}