using Semaphores;
using TUnit.Core.Interfaces;
using TUnit.Engine.Data;

namespace TUnit.Engine.Services;

public class ParallelLimitProvider
{
    private static readonly GetOnlyDictionary<Type, IParallelLimit> Limits = new();
    private static readonly GetOnlyDictionary<Type, AsyncSemaphore> Locks = new();

    public static TParallelLimit GetParallelLimit<TParallelLimit>() where TParallelLimit : IParallelLimit, new()
    {
        return (TParallelLimit) Limits.GetOrAdd(typeof(TParallelLimit), _ => new TParallelLimit());
    }
    
    internal AsyncSemaphore GetLock(IParallelLimit parallelLimit)
    {
        if (parallelLimit.Limit <= 0)
        {
            throw new Exception("Parallel Limit must be positive");
        }
        
        return Locks.GetOrAdd(parallelLimit.GetType(), _ => new AsyncSemaphore(parallelLimit.Limit));
    }
}