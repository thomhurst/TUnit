﻿using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal class DiscoveredTest<
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] 
    TTestClass
    > : DiscoveredTest
{
    private readonly ResettableLazy<TTestClass> _resettableLazyTestClassFactory;

    public DiscoveredTest(ResettableLazy<TTestClass> resettableLazyTestClassFactory)
    {
        _resettableLazyTestClassFactory = resettableLazyTestClassFactory;
    }
    
    public TTestClass TestClass => _resettableLazyTestClassFactory.Value;
    
    public required Func<TTestClass, CancellationToken, Task> TestBody { get; init; }

    public override async Task ExecuteTest(CancellationToken cancellationToken)
    {
        await TestExecutor.ExecuteTest(TestContext, () => TestBody.Invoke(TestClass, cancellationToken));
    }
    
    public override async Task ResetTestInstance()
    {
        await _resettableLazyTestClassFactory.ResetLazy();
    }
}

internal abstract class DiscoveredTest
{
    public required TestContext TestContext { get; init; }
    
    public required IBeforeTestAttribute[] BeforeTestAttributes { get; init; }
    public required IAfterTestAttribute[] AfterTestAttributes { get; init; }

    public abstract Task ExecuteTest(CancellationToken cancellationToken);

    public abstract Task ResetTestInstance();
    
    public TestDetails TestDetails => TestContext.TestDetails;
    
    public required ITestExecutor TestExecutor { get; internal set; }
    
    public required IClassConstructor? ClassConstructor { get; set; }
    
    public IHookExecutor? HookExecutor { get; internal set; }
    
    public bool IsStarted { get; set; }
}