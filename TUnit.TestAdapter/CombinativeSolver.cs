using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.TestAdapter;

public class CombinativeSolver
{
    public IEnumerable<IEnumerable<object?>> GetCombinativeArgumentsList(IEnumerable<IReadOnlyList<object?>> elements)
    {
        IEnumerable<IEnumerable<object?>> Seed() { yield return Enumerable.Empty<object?>(); }

        return elements.Aggregate(Seed(), (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }
}