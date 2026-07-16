using System.Diagnostics.CodeAnalysis;

namespace TUnit.TestProject;

[UnconditionalSuppressMessage("Usage", "TUnit0033:Conflicting DependsOn attributes")]
public class CircularDependencyTestVerification
{
    [Test, DependsOn(nameof(TestB))]
    public async Task TestA()
    {
        Console.WriteLine("TestA executing - this should not happen!");
        await Task.CompletedTask;
    }

    [Test, DependsOn(nameof(TestA))]
    public async Task TestB()
    {
        Console.WriteLine("TestB executing - this should not happen!");
        await Task.CompletedTask;
    }

    [Test, DependsOn(nameof(TestE))]
    public async Task TestC()
    {
        Console.WriteLine("TestC executing - this should not happen!");
        await Task.CompletedTask;
    }

    [Test, DependsOn(nameof(TestC))]
    public async Task TestD()
    {
        Console.WriteLine("TestD executing - this should not happen!");
        await Task.CompletedTask;
    }

    [Test, DependsOn(nameof(TestD))]
    public async Task TestE()
    {
        Console.WriteLine("TestE executing - this should not happen!");
        await Task.CompletedTask;
    }
    
    [Test]
    public async Task TestIndependent()
    {
        Console.WriteLine("TestIndependent executing - this should work fine!");
        await Task.CompletedTask;
    }
}