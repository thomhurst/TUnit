using TUnit.NugetTester.Library;
using TUnit.Core.Logging;

namespace TUnit.NugetTester;

public enum Enum1
{
    Value1,
    Value2
}

public class MyTests : TestBase
{
    [Test]
    public void Test()
    {
        TestContext.Current!.GetDefaultLogger().LogInformation("Blah");
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public void DataTest(int value)
    {
        TestContext.Current!.GetDefaultLogger().LogInformation(value.ToString());
    }

    [Test]
    [Arguments(Enum1.Value1)]
    public async Task TestMethod(Enum1 i)
    {
        await Assert.That(i).IsDefined();
    }

    [Test]
    [Arguments(new int[] { 1, 2, 3 })]
    public async Task TestMethod2(int[] i)
    {
        await Assert.That(i).HasCount(3);
    }
}
