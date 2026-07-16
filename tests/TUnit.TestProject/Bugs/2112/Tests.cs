using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2112;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [Arguments(0, 1L)] // this is ok
    [Arguments(0, 1L)] // Fixed: Changed int to long to match parameter type
    public void Test(int a, params long[] arr)
    {
    }

    [Test]
    [Arguments(0, 1L, 2L, 3L)] // this is ok
    [Arguments(0, 1L, 2L, 3L)] // Fixed: Changed ints to longs to match parameter type
    public void Test2(int a, params long[] arr)
    {
    }
}
