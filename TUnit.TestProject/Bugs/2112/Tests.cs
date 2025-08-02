using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2112;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [Arguments(0, 1L)] // this is ok
    [Arguments(0, 1)] // Error TUnit0001 : Attribute argument types 'int' don't match method parameter types 'long[]'
    public void Test(int a, params long[] arr)
    {
    }

    [Test]
    [Arguments(0, 1L, 2L, 3L)] // this is ok
    [Arguments(0, 1, 2, 3)] // Error TUnit0001 : Attribute argument types 'int' don't match method parameter types 'long[]'
    public void Test2(int a, params long[] arr)
    {
    }
}
