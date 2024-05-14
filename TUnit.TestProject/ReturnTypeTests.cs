using TUnit.Core;

namespace TUnit.TestProject;

public class ReturnTypeTests
{
    [Test]
    public void Test1()
    {
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
        return ValueTask.CompletedTask;
    }
    
    [Test]
    public ValueTask<int> Test6()
    {
        return ValueTask.FromResult(1);
    }
}