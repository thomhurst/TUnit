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
        return default(ValueTask);
    }

    [Test]
    public async ValueTask<int> Test6()
    {
        await Task.Delay(1); // Small delay to ensure truly async and avoid race conditions
        return 1;
    }
}
