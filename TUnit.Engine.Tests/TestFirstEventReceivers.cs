using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Tests;

public class FirstEventReceiversTest : IFirstTestInAssemblyEventReceiver, IFirstTestInClassEventReceiver
{
    public static readonly ConcurrentBag<string> AssemblyEvents = new();
    public static readonly ConcurrentBag<string> ClassEvents = new();
    
    public int Order => 0;

    public ValueTask OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        var assemblyName = context.Assembly.GetName().FullName ?? "Unknown";
        var message = $"FirstAssembly: {assemblyName} - Test: {testContext.TestDetails.TestName}";
        AssemblyEvents.Add(message);
        Console.WriteLine(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnFirstTestInClass(ClassHookContext context, TestContext testContext)
    {
        var className = context.ClassType.FullName ?? "Unknown";
        var message = $"FirstClass: {className} - Test: {testContext.TestDetails.TestName}";
        ClassEvents.Add(message);
        Console.WriteLine(message);
        return ValueTask.CompletedTask;
    }
}

public class TestClass1
{
    [Test]
    public Task Test1()
    {
        Console.WriteLine("TestClass1.Test1 executing");
        return Task.CompletedTask;
    }

    [Test] 
    public Task Test2()
    {
        Console.WriteLine("TestClass1.Test2 executing");
        return Task.CompletedTask;
    }
}

public class TestClass2
{
    [Test]
    public Task Test3()
    {
        Console.WriteLine("TestClass2.Test3 executing");
        return Task.CompletedTask;
    }

    [Test]
    public Task Test4()
    {
        Console.WriteLine("TestClass2.Test4 executing");
        return Task.CompletedTask;
    }
}

public class VerificationTest
{
    [Test]
    public void VerifyFirstEventsCalled()
    {
        Console.WriteLine($"Assembly events called {FirstEventReceiversTest.AssemblyEvents.Count} times:");
        foreach (var evt in FirstEventReceiversTest.AssemblyEvents)
        {
            Console.WriteLine($"  {evt}");
        }

        Console.WriteLine($"Class events called {FirstEventReceiversTest.ClassEvents.Count} times:");
        foreach (var evt in FirstEventReceiversTest.ClassEvents)
        {
            Console.WriteLine($"  {evt}");
        }

        // Expected: Assembly event called exactly once
        // Expected: Class event called exactly twice (once per class)
        if (FirstEventReceiversTest.AssemblyEvents.Count != 1)
        {
            throw new InvalidOperationException($"Expected assembly event to be called exactly once, but was called {FirstEventReceiversTest.AssemblyEvents.Count} times");
        }

        if (FirstEventReceiversTest.ClassEvents.Count != 3) // TestClass1, TestClass2, VerificationTest
        {
            throw new InvalidOperationException($"Expected class event to be called exactly 3 times (once per class), but was called {FirstEventReceiversTest.ClassEvents.Count} times");
        }
    }
}