using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

public sealed class DefaultPlaywrightParallelLimiter : IParallelLimit
{
    private static readonly int StaticallyInitializedLimit = GetLimit();
    
    public int Limit => StaticallyInitializedLimit;

    private static int GetLimit()
    {
        var limit = Math.Max(Environment.ProcessorCount, 2);
        
        Console.WriteLine(@$"Default playwright parallel limiter set to {limit}");

        return limit;
    }
}