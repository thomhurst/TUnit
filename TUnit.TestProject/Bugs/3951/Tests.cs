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
    public required ErrContext Fixture { get; set; }
    public ValueTask DisposeAsync() => default;
    public Task InitializeAsync() => Task.CompletedTask;
}

public class ErrTest
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

    [MyTestGeneratorAttribute<MyType>]
    [Test]
    public async Task MyTest2(MyType t)
    {
        await Assert.That(t.GetType()).IsAssignableTo<MyType>();
    }
}
