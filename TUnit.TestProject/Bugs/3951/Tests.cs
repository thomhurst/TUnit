using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3951;


public sealed class MyTestGeneratorAttribute<T> : DataSourceGeneratorAttribute<T> where T : MyType, new()
{
    protected override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata) => [() => new T()];
}

public class MyType;

public class ErrContext: IAsyncInitializer, IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
    public Task InitializeAsync() => Task.CompletedTask;
}

public class ErrFixture<T> : IAsyncDisposable, IAsyncInitializer
{
    [ClassDataSource<ErrContext>(Shared = SharedType.PerClass)]
    public required ErrContext Fixture { get; set; }
    public ValueTask DisposeAsync() => default;
    public Task InitializeAsync() => Task.CompletedTask;
}

/// <summary>
/// Test for instance data source that depends on property injection.
/// Note: MyTest uses an instance property data source that depends on property-injected
/// fixtures. This works in source-generated mode but not in reflection mode because:
/// 1. Property injection happens after instance creation
/// 2. Data sources are evaluated before initialization in reflection mode
/// Therefore, MyTest is split into a separate class without [EngineTest(ExpectedResult.Pass)].
/// </summary>
public class ErrTest_InstanceDataSource
{
    [ClassDataSource<ErrFixture<MyType>>(Shared = SharedType.PerClass)]
    public required ErrFixture<MyType> Fixture { get; init; }

    public IEnumerable<Func<ErrContext>> TestExecutions => [() => Fixture.Fixture];

    [MethodDataSource("TestExecutions")]
    [Test]
    public async Task MyTest(ErrContext context)
    {
        await Assert.That(context.GetType()).IsNotAssignableTo<MyType>();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ErrTest
{
    [MyTestGeneratorAttribute<MyType>]
    [Test]
    public async Task MyTest2(MyType t)
    {
        await Assert.That(t).IsAssignableTo<MyType>();
    }
}
