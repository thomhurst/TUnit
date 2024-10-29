using TUnit.Core.Data;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class ParallelLimitLockProvider
{
    private readonly GetOnlyDictionary<Type, SemaphoreSlim> _locks = new();
    
    internal SemaphoreSlim GetLock(IParallelLimit parallelLimit)
    {
        if (parallelLimit.Limit <= 0)
        {
            throw new Exception("Parallel Limit must be positive");
        }
        
        return _locks.GetOrAdd(parallelLimit.GetType(), _ => new SemaphoreSlim(parallelLimit.Limit, parallelLimit.Limit));
    }
}