using TUnit.Core.Data;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

public class ParallelLimitProvider
{
    private static readonly GetOnlyDictionary<Type, IParallelLimit> Limits = new();
    private static readonly GetOnlyDictionary<Type, SemaphoreSlim> Locks = new();

    public static TParallelLimit GetParallelLimit<TParallelLimit>() where TParallelLimit : IParallelLimit, new()
    {
        return (TParallelLimit) Limits.GetOrAdd(typeof(TParallelLimit), _ => new TParallelLimit());
    }
    
    internal SemaphoreSlim GetLock(IParallelLimit parallelLimit)
    {
        if (parallelLimit.Limit <= 0)
        {
            throw new Exception("Parallel Limit must be positive");
        }
        
        return Locks.GetOrAdd(parallelLimit.GetType(), _ => new SemaphoreSlim(parallelLimit.Limit, parallelLimit.Limit));
    }
}