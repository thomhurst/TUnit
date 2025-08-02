using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

// Test that scoped event receiver attributes only fire once
[TestStartLogger(source: "assembly")]
[TestEndLogger(source: "assembly")]
public class ScopedEventReceiverTests
{
    internal static readonly object _lock = new();
    internal static readonly Dictionary<string, List<string>> _testStartEvents = new();
    internal static readonly Dictionary<string, List<string>> _testEndEvents = new();
    
    internal static void RecordStartEvent(string testName, string source)
    {
        lock (_lock)
        {
            if (!_testStartEvents.ContainsKey(testName))
            {
                _testStartEvents[testName] = new List<string>();
            }
            _testStartEvents[testName].Add(source);
        }
    }
    
    internal static void RecordEndEvent(string testName, string source)
    {
        lock (_lock)
        {
            if (!_testEndEvents.ContainsKey(testName))
            {
                _testEndEvents[testName] = new List<string>();
            }
            _testEndEvents[testName].Add(source);
        }
    }

    [Test]
    [TestStartLogger(source: "method")]
    [TestEndLogger(source: "method")]
    public async Task TestWithMethodLevelEventReceivers()
    {
        // Wait a bit to ensure events have been recorded
        await Task.Delay(100);
        
        // Method-level attributes should override assembly level
        List<string> startEvents;
        List<string> endEvents;
        
        lock (_lock)
        {
            startEvents = _testStartEvents.GetValueOrDefault(nameof(TestWithMethodLevelEventReceivers)) ?? new List<string>();
            endEvents = _testEndEvents.GetValueOrDefault(nameof(TestWithMethodLevelEventReceivers)) ?? new List<string>();
        }
        
        
        await Assert.That(startEvents).Contains("method");
        await Assert.That(startEvents).DoesNotContain("assembly");
        // EndEvents might not be populated yet as they fire after the test
    }

    [Test]
    public async Task TestWithClassLevelEventReceivers()
    {
        // Wait a bit to ensure events have been recorded
        await Task.Delay(100);
        
        // Should use assembly-level since no class-level overrides exist
        List<string> startEvents;
        List<string> endEvents;
        
        lock (_lock)
        {
            startEvents = _testStartEvents.GetValueOrDefault(nameof(TestWithClassLevelEventReceivers)) ?? new List<string>();
            endEvents = _testEndEvents.GetValueOrDefault(nameof(TestWithClassLevelEventReceivers)) ?? new List<string>();
        }
        
        
        await Assert.That(startEvents).Contains("assembly");
        await Assert.That(startEvents).DoesNotContain("method");
    }
}

[TestStartLogger(source: "class")]
[TestEndLogger(source: "class")]
public class ScopedEventReceiverTests2
{
    [Test]
    public async Task TestWithOnlyClassLevelEventReceivers()
    {
        // Wait a bit to ensure events have been recorded
        await Task.Delay(100);
        
        // Should use class-level since it overrides assembly level
        var methodName = nameof(TestWithOnlyClassLevelEventReceivers);
        var startEvents = new List<string>();
        var endEvents = new List<string>();
        
        lock (ScopedEventReceiverTests._lock)
        {
            if (ScopedEventReceiverTests._testStartEvents.ContainsKey(methodName))
            {
                startEvents = ScopedEventReceiverTests._testStartEvents[methodName];
            }
            if (ScopedEventReceiverTests._testEndEvents.ContainsKey(methodName))
            {
                endEvents = ScopedEventReceiverTests._testEndEvents[methodName];
            }
        }
        
        
        // Assert outside of lock
        await Assert.That(startEvents).Contains("class");
        await Assert.That(startEvents).DoesNotContain("assembly");
    }
}

// Custom event receiver attributes that implement IScopedAttribute
public class TestStartLoggerAttribute : Attribute, ITestStartEventReceiver, IScopedAttribute<TestStartLoggerAttribute>
{
    private readonly string _source;

    public TestStartLoggerAttribute(string source)
    {
        _source = source;
    }

    public int Order => 0;

    public ValueTask OnTestStart(TestContext context)
    {
        ScopedEventReceiverTests.RecordStartEvent(context.TestDetails.MethodName, _source);
        return default;
    }
}

public class TestEndLoggerAttribute : Attribute, ITestEndEventReceiver, IScopedAttribute<TestEndLoggerAttribute>
{
    private readonly string _source;

    public TestEndLoggerAttribute(string source)
    {
        _source = source;
    }

    public int Order => 0;

    public ValueTask OnTestEnd(TestContext context)
    {
        ScopedEventReceiverTests.RecordEndEvent(context.TestDetails.MethodName, _source);
        return default;
    }
}