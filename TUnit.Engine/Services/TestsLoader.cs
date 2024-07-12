namespace TUnit.Engine.Services;

internal class TestsLoader
{
    public ParallelQuery<DiscoveredTest> GetTests()
    {
        return TestDictionary.GetAllTests().AsParallel();
    }
}