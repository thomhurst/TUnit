using System.Diagnostics;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal record DiscoveredTest<
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis
        .DynamicallyAccessedMemberTypes.PublicConstructors)]
    TTestClass
>(ResettableLazy<TTestClass> ResettableLazyTestClassFactory) : DiscoveredTest where TTestClass : class
{
    public TTestClass ClassMetadata => ResettableLazyTestClassFactory.Value;
    
    public required Func<TTestClass, CancellationToken, ValueTask> TestBody { get; init; }

    public override async ValueTask ExecuteTest(CancellationToken cancellationToken)
    {
        TestContext.CancellationToken = cancellationToken;
        await TestExecutor.ExecuteTest(TestContext, () => TestBody.Invoke(ClassMetadata, cancellationToken));
    }
    
    public override async ValueTask ResetTestInstance()
    {
        await ResettableLazyTestClassFactory.ResetLazy();
    }

    public override IClassConstructor? ClassConstructor => ResettableLazyTestClassFactory.ClassConstructor;
}

[DebuggerDisplay("{TestDetails.TestId}")]
internal abstract record DiscoveredTest : IComparable<DiscoveredTest>, IComparable
{
    public required TestContext TestContext { get; init; }

    public abstract ValueTask ExecuteTest(CancellationToken cancellationToken);

    public abstract ValueTask ResetTestInstance();
    
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