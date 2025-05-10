#pragma warning disable

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._1187;

public record TestRecord(Guid AppId, int X, int Y);

public static class TestData
{
    public static Guid AppId1 { get; } = Guid.NewGuid();

    public static IEnumerable<TestRecord> App1Data()
    {
        yield return new TestRecord(AppId1, 7, 2);
        yield return new TestRecord(AppId1, 2, 1);
        yield return new TestRecord(AppId1, 1, 0);
        yield return new TestRecord(Guid.NewGuid(), 0, 1);
    }
}

public class Context : IAsyncInitializer
{
    public Guid Id { get; } = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        Console.WriteLine(@"in context init async");
        await Task.Delay(2);
        Console.WriteLine(@"context init async done");
    }
}

[ClassDataSource<Context>(Shared = SharedType.None)]
[MethodDataSource(nameof(Contexts))]
public class Tests(Context ctx)
{
    private static List<Guid> Ids { get; } = [];
    private static readonly SemaphoreSlim Lock = new(1, 1);

    [Test]
    [MethodDataSource(typeof(TestData), nameof(TestData.App1Data))]
    public async Task Test1(TestRecord r)
    {
        await AssertUniqueContext(ctx.Id);
    }
    
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Test2(int a)
    {
        await AssertUniqueContext(ctx.Id);
    }
    
    [Test]
    [MethodDataSource(nameof(Contexts))]
    public async Task Test3(Context ctx2)
    {
        await AssertUniqueContext(ctx.Id);
        await AssertUniqueContext(ctx2.Id);
    }

    private async Task AssertUniqueContext(Guid guid)
    {
        await Lock.WaitAsync();

        try
        {
            await Assert.That(Ids).DoesNotContain(guid);
            Ids.Add(guid);
        }
        finally
        {
            Lock.Release();
        }
    }

    public static IEnumerable<Func<Context>> Contexts()
    {
        yield return () => new Context();
        yield return () => new Context();
    }
}