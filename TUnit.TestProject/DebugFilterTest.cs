using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DebugFilterTest
{
    [Test]
    public void SimpleTestWithEngineTestAttribute()
    {
        // This test should be picked up by the filter
        Console.WriteLine("Test executed!");
    }
}