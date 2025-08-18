using System.Collections.Concurrent;
using Shouldly;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Tests;

/// <summary>
/// Test to validate that IFirstTestInAssemblyEventReceiver and IFirstTestInClassEventReceiver
/// are called exactly once per scope, addressing the regression reported in issue #2916.
/// </summary>
public class FirstEventReceiversRegressionTest : IFirstTestInAssemblyEventReceiver, IFirstTestInClassEventReceiver
{
    private static readonly ConcurrentBag<string> _assemblyEvents = new();
    private static readonly ConcurrentBag<string> _classEvents = new();
    
    public int Order => 0;

    ValueTask IFirstTestInAssemblyEventReceiver.OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        var assemblyName = context.Assembly.GetName().FullName ?? "Unknown";
        var eventInfo = $"Assembly: {assemblyName}";
        _assemblyEvents.Add(eventInfo);
        return ValueTask.CompletedTask;
    }

    ValueTask IFirstTestInClassEventReceiver.OnFirstTestInClass(ClassHookContext context, TestContext testContext)
    {
        var className = context.ClassType.FullName ?? "Unknown";
        var eventInfo = $"Class: {className}";
        _classEvents.Add(eventInfo);
        return ValueTask.CompletedTask;
    }

    [Test]
    public void EventReceiversCalledOncePerScope()
    {
        // This test validates that the fix is working correctly
        // The events should be called exactly once per assembly and once per test class
        
        var assemblyEventCount = _assemblyEvents.Count;
        var classEventCount = _classEvents.Count;
        
        // We expect exactly one assembly event for this test assembly
        assemblyEventCount.ShouldBe(1);
        
        // We expect exactly one class event for this test class
        classEventCount.ShouldBe(1);
        
        // Verify the content makes sense
        var assemblyEvent = _assemblyEvents.Single();
        assemblyEvent.ShouldContain("TUnit.Engine.Tests");
        
        var classEvent = _classEvents.Single();
        classEvent.ShouldContain("FirstEventReceiversRegressionTest");
    }
}