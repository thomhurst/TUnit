using System;
using System.Threading.Tasks;

namespace TUnit.TestProject.Bugs._2755;

public class Test1
{
    [Test]
    public Task TestMethod()
    {
        Console.WriteLine("TestMethod executed - this should run normally");
        return Task.CompletedTask;
    }

    [Test, Explicit]
    public Task TestMethod2()
    {
        // This should only run when explicitly requested
        throw new NotImplementedException("TestMethod2 should not be executed unless explicitly requested!");
    }
}

[Explicit]
public class ExplicitClass
{
    [Test]
    public Task TestInExplicitClass()
    {
        // This test is in an explicit class, so it should only run when the class is explicitly requested
        Console.WriteLine("TestInExplicitClass executed - class was explicitly requested");
        return Task.CompletedTask;
    }

    [Test]
    public Task AnotherTestInExplicitClass()
    {
        Console.WriteLine("AnotherTestInExplicitClass executed - class was explicitly requested");
        return Task.CompletedTask;
    }
}

public class MixedTests
{
    [Test]
    public Task NormalTest()
    {
        Console.WriteLine("NormalTest executed");
        return Task.CompletedTask;
    }

    [Test, Explicit]
    public Task ExplicitTestInNormalClass()
    {
        throw new NotImplementedException("ExplicitTestInNormalClass should not run unless explicitly requested!");
    }

    [Test, Skip("Skipped for testing")]
    public Task SkippedTest()
    {
        throw new NotImplementedException("This should never run - it's skipped");
    }

    [Test, Explicit, Skip("Both explicit and skip")]
    public Task ExplicitAndSkippedTest()
    {
        throw new NotImplementedException("This should never run - Skip takes precedence over Explicit");
    }
}