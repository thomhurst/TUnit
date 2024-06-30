using TUnit.Core;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestsLoader
{
    public IEnumerable<DiscoveredTest> GetTests()
    {
        return TestDictionary.GetAllTests().Select(x => new DiscoveredTest(x));
    }
}