using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record TestContextEvents : 
    IAsyncInitializer,
    IAsyncDisposable,
    ITestRegisteredEventReceiver,
    ITestStartEventReceiver,
    ITestEndEventReceiver,
    ITestSkippedEventReceiver,
    ILastTestInClassEventReceiver,
    ILastTestInAssemblyEventReceiver,
    ILastTestInTestSessionEventReceiver,
    ITestRetryEventReceiver
{
    public int Order => int.MaxValue / 2;
    
    public AsyncEvent<TestContext>? OnDispose { get; set; }
    public AsyncEvent<TestRegisteredContext>? OnTestRegistered { get; set; }
    public AsyncEvent<TestContext>? OnInitialize { get; set; }
    public AsyncEvent<BeforeTestContext>? OnTestStart { get; set; }
    public AsyncEvent<TestContext>? OnTestEnd { get; set; }
    public AsyncEvent<TestContext>? OnTestSkipped { get; set; }
    public AsyncEvent<(ClassHookContext, TestContext)>? OnLastTestInClass { get; set; }
    public AsyncEvent<(AssemblyHookContext, TestContext)>? OnLastTestInAssembly { get; set; }
    public AsyncEvent<(TestSessionContext, TestContext)>? OnLastTestInTestSession { get; set; }
    public AsyncEvent<(TestContext, int RetryAttempt)>? OnTestRetry { get; set; }

    ValueTask ITestRegisteredEventReceiver.OnTestRegistered(TestRegisteredContext context)
    {
        return OnTestRegistered?.InvokeAsync(this, context) ?? default;
    }

    ValueTask ITestStartEventReceiver.OnTestStart(BeforeTestContext beforeTestContext)
    {
        return OnTestStart?.InvokeAsync(this, beforeTestContext) ?? default;
    }

    ValueTask ITestEndEventReceiver.OnTestEnd(TestContext testContext)
    {
        return OnTestEnd?.InvokeAsync(this, testContext) ?? default;
    }
    
    ValueTask ITestSkippedEventReceiver.OnTestSkipped(TestContext testContext)
    {
        return OnTestSkipped?.InvokeAsync(this, testContext) ?? default;
    }

    ValueTask ILastTestInClassEventReceiver.OnLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        return OnLastTestInClass?.InvokeAsync(this, (context, testContext)) ?? default;
    }

    ValueTask ILastTestInAssemblyEventReceiver.OnLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        return OnLastTestInAssembly?.InvokeAsync(this, (context, testContext)) ?? default;
    }

    ValueTask ILastTestInTestSessionEventReceiver.OnLastTestInTestSession(TestSessionContext context, TestContext testContext)
    {
        return OnLastTestInTestSession?.InvokeAsync(this, (context, testContext)) ?? default;
    }

    ValueTask ITestRetryEventReceiver.OnTestRetry(TestContext testContext, int retryAttempt)
    {
        return OnTestRetry?.InvokeAsync(this, (testContext, retryAttempt)) ?? default;
    }

    public void OnTestStartSynchronous(BeforeTestContext beforeTestContext)
    {
    }

    public async Task InitializeAsync()
    {
        if (OnInitialize != null)
        {
            await OnInitialize.InvokeAsync(this, TestContext.Current!);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (OnDispose != null)
        {
            await OnDispose.InvokeAsync(this, TestContext.Current!);
        }
    }
}