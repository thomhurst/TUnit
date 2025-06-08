using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents the events for a test context.
/// </summary>
public record TestContextEvents
{
    public AsyncEvent<TestContext>? OnDispose { get; set; }
    public AsyncEvent<TestRegisteredContext>? OnTestRegistered { get; set; }
    public AsyncEvent<TestContext>? OnInitialize { get; set; }
    public AsyncEvent<BeforeTestContext>? OnTestStart { get; set; }
    public AsyncEvent<AfterTestContext>? OnTestEnd { get; set; }
    public AsyncEvent<TestContext>? OnTestSkipped { get; set; }
    public AsyncEvent<(TestSessionContext, TestContext)>? OnFirstTestInTestSession { get; set; }
    public AsyncEvent<(AssemblyHookContext, TestContext)>? OnFirstTestInAssembly { get; set; }
    public AsyncEvent<(ClassHookContext, TestContext)>? OnFirstTestInClass { get; set; }
    public AsyncEvent<(ClassHookContext, TestContext)>? OnLastTestInClass { get; set; }
    public AsyncEvent<(AssemblyHookContext, TestContext)>? OnLastTestInAssembly { get; set; }
    public AsyncEvent<(TestSessionContext, TestContext)>? OnLastTestInTestSession { get; set; }
    public AsyncEvent<(TestContext, int RetryAttempt)>? OnTestRetry { get; set; }
}
