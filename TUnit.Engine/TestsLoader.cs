using TUnit.Core;

namespace TUnit.Engine;

internal class TestsLoader
{
    public IEnumerable<TestInformation> GetTests()
    {
        return TestDictionary.GetAllTestDetails();
    }
}