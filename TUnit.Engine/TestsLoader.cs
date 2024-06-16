using TUnit.Core;

namespace TUnit.Engine;

internal class TestsLoader
{
    public IEnumerable<DiscoveredTest> GetTests()
    {
        return TestDictionary.GetAllTestDetails().Select(x => new DiscoveredTest(x));
    }
}