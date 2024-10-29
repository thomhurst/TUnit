using TUnit.Core.Data;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class ParallelLimitLockProvider
{
    private static readonly GetOnlyDictionary<Type, SemaphoreSlim> Locks = new();
    
    internal SemaphoreSlim GetLock(IParallelLimit parallelLimit)
    {
        if (parallelLimit.Limit <= 0)
        {
            throw new Exception("Parallel Limit must be positive");
        }
        
        return Locks.GetOrAdd(parallelLimit.GetType(), _ => new SemaphoreSlim(parallelLimit.Limit, parallelLimit.Limit));
    }
}