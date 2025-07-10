using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal sealed class EventReceiverOrchestrator
{
    private readonly TUnitFrameworkLogger _logger;
    
    // Track which assemblies/classes/sessions have had their "first" event invoked
    private readonly ConcurrentDictionary<string, bool> _firstTestInAssemblyInvoked = new();
    private readonly ConcurrentDictionary<Type, bool> _firstTestInClassInvoked = new();
    private int _firstTestInSessionInvoked = 0;
    
    // Track remaining test counts for "last" events
    private readonly ConcurrentDictionary<string, int> _assemblyTestCounts = new();
    private readonly ConcurrentDictionary<Type, int> _classTestCounts = new();
    private int _sessionTestCount = 0;

    public EventReceiverOrchestrator(TUnitFrameworkLogger logger)
    {
        _logger = logger;
    }

    public async ValueTask InvokeTestStartEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestStartEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnTestStart(context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test start event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeTestEndEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestEndEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnTestEnd(context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test end event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeTestSkippedEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestSkippedEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnTestSkipped(context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test skipped event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeTestDiscoveryEventReceiversAsync(TestContext context, DiscoveredTestContext discoveredContext, CancellationToken cancellationToken)
    {
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestDiscoveryEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnTestDiscovered(discoveredContext);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test discovery event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeTestRegisteredEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        var registeredContext = new TestRegisteredContext(context);
        
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestRegisteredEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnTestRegistered(registeredContext);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test registered event receiver: {ex.Message}");
            }
        }
    }

    public void InitializeTestCounts(IEnumerable<TestContext> allTests)
    {
        foreach (var test in allTests)
        {
            var assemblyName = test.TestDetails.ClassType.Assembly.FullName ?? "";
            _assemblyTestCounts.AddOrUpdate(assemblyName, 1, (_, count) => count + 1);
            
            var classType = test.TestDetails.ClassType;
            _classTestCounts.AddOrUpdate(classType, 1, (_, count) => count + 1);
        }
        
        _sessionTestCount = allTests.Count();
    }

    public async ValueTask InvokeFirstTestInSessionEventReceiversAsync(TestContext context, TestSessionContext sessionContext, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _firstTestInSessionInvoked, 1, 0) != 0)
        {
            return;
        }

        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<IFirstTestInTestSessionEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnFirstTestInTestSession(sessionContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in first test in session event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeFirstTestInAssemblyEventReceiversAsync(TestContext context, AssemblyHookContext assemblyContext, CancellationToken cancellationToken)
    {
        var assemblyName = context.TestDetails.ClassType.Assembly.FullName ?? "";
        if (!_firstTestInAssemblyInvoked.TryAdd(assemblyName, true))
        {
            return;
        }

        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<IFirstTestInAssemblyEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnFirstTestInAssembly(assemblyContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in first test in assembly event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeFirstTestInClassEventReceiversAsync(TestContext context, ClassHookContext classContext, CancellationToken cancellationToken)
    {
        var classType = context.TestDetails.ClassType;
        if (!_firstTestInClassInvoked.TryAdd(classType, true))
        {
            return;
        }

        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<IFirstTestInClassEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnFirstTestInClass(classContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in first test in class event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeLastTestInSessionEventReceiversAsync(TestContext context, TestSessionContext sessionContext, CancellationToken cancellationToken)
    {
        var remaining = Interlocked.Decrement(ref _sessionTestCount);
        if (remaining > 0)
        {
            return;
        }

        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ILastTestInTestSessionEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnLastTestInTestSession(sessionContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in session event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeLastTestInAssemblyEventReceiversAsync(TestContext context, AssemblyHookContext assemblyContext, CancellationToken cancellationToken)
    {
        var assemblyName = context.TestDetails.ClassType.Assembly.FullName ?? "";
        var remaining = _assemblyTestCounts.AddOrUpdate(assemblyName, 0, (_, count) => count - 1);
        if (remaining > 0)
        {
            return;
        }

        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ILastTestInAssemblyEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnLastTestInAssembly(assemblyContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in assembly event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeLastTestInClassEventReceiversAsync(TestContext context, ClassHookContext classContext, CancellationToken cancellationToken)
    {
        var classType = context.TestDetails.ClassType;
        var remaining = _classTestCounts.AddOrUpdate(classType, 0, (_, count) => count - 1);
        if (remaining > 0)
        {
            return;
        }

        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ILastTestInClassEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnLastTestInClass(classContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in class event receiver: {ex.Message}");
            }
        }
    }
}