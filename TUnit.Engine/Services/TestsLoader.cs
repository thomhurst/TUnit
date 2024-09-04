using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestsLoader
{
    public IEnumerable<DiscoveredTest> GetTests()
    {
        return TestDictionary.GetAllTests();
    }
}