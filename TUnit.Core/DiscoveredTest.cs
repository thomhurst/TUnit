using System.Diagnostics;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal record DiscoveredTest<
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis
        .DynamicallyAccessedMemberTypes.PublicConstructors)]
    TTestClass
>(ResettableLazy<TTestClass> ResettableLazyTestClassFactory) : DiscoveredTest where TTestClass : class
{
    public TTestClass TestClass => ResettableLazyTestClassFactory.Value;
    
    public required Func<TTestClass, CancellationToken, Task> TestBody { get; init; }

    public override async Task ExecuteTest(CancellationToken cancellationToken)
    {
        TestContext.CancellationToken = cancellationToken;
        await TestExecutor.ExecuteTest(TestContext, () => TestBody.Invoke(TestClass, cancellationToken));
    }
    
    public override async Task ResetTestInstance()
    {
        await ResettableLazyTestClassFactory.ResetLazy();
    }

    public override IClassConstructor? ClassConstructor => ResettableLazyTestClassFactory.ClassConstructor;
}

[DebuggerDisplay("{TestDetails.TestId}")]
internal abstract record DiscoveredTest : IComparable<DiscoveredTest>, IComparable
{
    public required TestContext TestContext { get; init; }

    public abstract Task ExecuteTest(CancellationToken cancellationToken);

    public abstract Task ResetTestInstance();
    
    public TestDetails TestDetails => TestContext.TestDetails;
    
    public ITestExecutor TestExecutor { get; internal set; } = DefaultExecutor.Instance;
    
    public abstract IClassConstructor? ClassConstructor { get; }
    
    public IHookExecutor? HookExecutor { get; internal set; }

    internal Dependency[] Dependencies { get; set; } = [];

    public virtual bool Equals(DiscoveredTest? other)
    {
        return other?.TestDetails.TestId == TestDetails.TestId;
    }

    public override int GetHashCode()
    {
        return TestDetails.TestId.GetHashCode();
    }

    public int CompareTo(object? obj)
    {
        return CompareTo(obj as DiscoveredTest);
    }

    public int CompareTo(DiscoveredTest? other)
    {
        return string.Compare(other?.TestDetails.TestId, TestDetails.TestId, StringComparison.Ordinal);
    }
}