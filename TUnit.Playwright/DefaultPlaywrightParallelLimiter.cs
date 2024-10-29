using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

public class DefaultPlaywrightParallelLimiter : IParallelLimit
{
    private static int? _limit;

    public int Limit => _limit ??= GetLimit() ;

    private static int GetLimit()
    {
        var limit = Environment.ProcessorCount * 2;
        
        Console.WriteLine($"Default playwright parallel limiter set to {limit}");

        return limit;
    }
}