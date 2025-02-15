namespace TUnit.TestProject.ComplexDependsOn;

public class BaseClass
{
    [Test]
    public async Task Test1()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public async Task Test2()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public async Task Test3()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public async Task Test4()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public async Task Test5()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}