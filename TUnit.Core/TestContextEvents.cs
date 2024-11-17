using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record TestContextEvents : 
    IDisposable,
    ITestRegisteredEventReceiver,
    ITestStartEventReceiver,
    ITestEndEventReceiver,
    ITestSkippedEventReceiver,
    ILastTestInClassEventReceiver,
    ILastTestInAssemblyEventReceiver,
    ILastTestInTestSessionEventReceiver,
    ITestRetryEventReceiver
{
    public EventHandler? OnDispose { get; set; }
    public AsyncEvent<TestRegisteredContext>? OnTestRegistered { get; set; }
    public AsyncEvent<BeforeTestContext>? OnTestStart { get; set; }
    public AsyncEvent<TestContext>? OnTestEnd { get; set; }
    public AsyncEvent<TestContext>? OnTestSkipped { get; set; }
    public AsyncEvent<(ClassHookContext, TestContext)>? OnLastTestInClass { get; set; }
    public AsyncEvent<(AssemblyHookContext, TestContext)>? OnLastTestInAssembly { get; set; }
    public AsyncEvent<(TestSessionContext, TestContext)>? OnLastTestInTestSession { get; set; }
    public AsyncEvent<(TestContext, int RetryAttempt)>? OnTestRetry { get; set; }

    ValueTask ITestRegisteredEventReceiver.OnTestRegistered(TestRegisteredContext context)
    {
        return OnTestRegistered?.InvokeAsync(this, context) ?? ValueTask.CompletedTask;
    }

    ValueTask ITestStartEventReceiver.OnTestStart(BeforeTestContext beforeTestContext)
    {
        return OnTestStart?.InvokeAsync(this, beforeTestContext) ?? ValueTask.CompletedTask;
    }

    ValueTask ITestEndEventReceiver.OnTestEnd(TestContext testContext)
    {
        return OnTestEnd?.InvokeAsync(this, testContext) ?? ValueTask.CompletedTask;
    }
    
    ValueTask ITestSkippedEventReceiver.OnTestSkipped(TestContext testContext)
    {
        return OnTestSkipped?.InvokeAsync(this, testContext) ?? ValueTask.CompletedTask;
    }

    public ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        return OnLastTestInClass?.InvokeAsync(this, (context, testContext)) ?? ValueTask.CompletedTask;
    }

    public ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        return OnLastTestInAssembly?.InvokeAsync(this, (context, testContext)) ?? ValueTask.CompletedTask;
    }

    public ValueTask IfLastTestInTestSession(TestSessionContext context, TestContext testContext)
    {
        return OnLastTestInTestSession?.InvokeAsync(this, (context, testContext)) ?? ValueTask.CompletedTask;
    }

    ValueTask ITestRetryEventReceiver.OnTestRetry(TestContext testContext, int retryAttempt)
    {
        return OnTestRetry?.InvokeAsync(this, (testContext, retryAttempt)) ?? ValueTask.CompletedTask;
    }
    
    public void Dispose()
    {
        OnDispose?.Invoke(this, EventArgs.Empty);
    }
}