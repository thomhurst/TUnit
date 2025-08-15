using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class FirstEventTracker : IFirstTestInAssemblyEventReceiver, IFirstTestInClassEventReceiver
{
    public static readonly ConcurrentBag<(string EventType, string Assembly, string Class, string Test, DateTime Timestamp)> Events = new();
    
    public int Order => 0;

    public ValueTask OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        var assembly = context.Assembly.GetName().FullName ?? "Unknown";
        var className = testContext.TestDetails.ClassType.FullName ?? "Unknown";
        var testName = testContext.TestDetails.TestName;
        
        Events.Add(("FirstAssembly", assembly, className, testName, DateTime.UtcNow));
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] FirstTestInAssembly: {assembly} - {className}.{testName}");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnFirstTestInClass(ClassHookContext context, TestContext testContext)
    {
        var assembly = context.AssemblyContext.Assembly.GetName().FullName ?? "Unknown";
        var className = context.ClassType.FullName ?? "Unknown";
        var testName = testContext.TestDetails.TestName;
        
        Events.Add(("FirstClass", assembly, className, testName, DateTime.UtcNow));
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] FirstTestInClass: {className} - {testName}");
        return ValueTask.CompletedTask;
    }
}

public class TestClassA
{
    [Test]
    public Task TestA1()
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] TestClassA.TestA1");
        return Task.CompletedTask;
    }

    [Test] 
    public Task TestA2()
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] TestClassA.TestA2");
        return Task.CompletedTask;
    }
}

public class TestClassB
{
    [Test]
    public Task TestB1()
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] TestClassB.TestB1");
        return Task.CompletedTask;
    }

    [Test]
    public Task TestB2()
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] TestClassB.TestB2");
        return Task.CompletedTask;
    }
}

public class EventValidationTest
{
    [Test]
    public void ValidateEventCallCounts()
    {
        var events = FirstEventTracker.Events.ToList();
        
        Console.WriteLine($"\nTotal events recorded: {events.Count}");
        foreach (var evt in events.OrderBy(e => e.Timestamp))
        {
            Console.WriteLine($"  {evt.EventType}: {evt.Assembly} - {evt.Class}.{evt.Test} at {evt.Timestamp:HH:mm:ss.fff}");
        }

        var assemblyEvents = events.Where(e => e.EventType == "FirstAssembly").ToList();
        var classEvents = events.Where(e => e.EventType == "FirstClass").ToList();

        Console.WriteLine($"\nAssembly events: {assemblyEvents.Count}");
        Console.WriteLine($"Class events: {classEvents.Count}");
        
        var uniqueAssemblies = assemblyEvents.Select(e => e.Assembly).Distinct().Count();
        var uniqueClasses = classEvents.Select(e => e.Class).Distinct().Count();
        
        Console.WriteLine($"Unique assemblies: {uniqueAssemblies}");
        Console.WriteLine($"Unique classes: {uniqueClasses}");

        // Validate expectations
        if (assemblyEvents.Count != uniqueAssemblies)
        {
            throw new InvalidOperationException($"Expected {uniqueAssemblies} assembly events but got {assemblyEvents.Count}");
        }
        
        if (classEvents.Count != uniqueClasses)
        {
            throw new InvalidOperationException($"Expected {uniqueClasses} class events but got {classEvents.Count}");
        }
    }
}