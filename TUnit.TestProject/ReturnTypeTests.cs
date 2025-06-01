using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ReturnTypeTests
{
    [Test]
    public void Test1()
    {
        // Dummy method
    }

    [Test]
    public int Test2()
    {
        return 1;
    }

    [Test]
    public Task Test3()
    {
        return Task.CompletedTask;
    }

    [Test]
    public Task<int> Test4()
    {
        return Task.FromResult(1);
    }

    [Test]
    public ValueTask Test5()
    {
        return default;
    }

    [Test]
    public ValueTask<int> Test6()
    {
        return new ValueTask<int>(1);
    }
}