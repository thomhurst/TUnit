using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class TupleLifecycleTests
{
    private static readonly ConcurrentBag<Fixture> Received = [];

    public static (Fixture, int) ValueRow() => (new Fixture(), 1);
    public static Tuple<Fixture, int> ReferenceRow() => Tuple.Create(new Fixture(), 1);
    public static ((Fixture, int), Fixture) NestedRow()
    {
        var fixture = new Fixture();
        return ((fixture, 1), fixture);
    }

    [Test, MethodDataSource(nameof(ValueRow))]
    public async Task Value_Tuple_Initializes_And_Disposes_Elements((Fixture, int) row)
        => await Check(row.Item1);

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public async Task Reference_Tuple_Initializes_And_Disposes_Elements(Tuple<Fixture, int> row)
        => await Check(row.Item1);

    [Test, MethodDataSource(nameof(NestedRow))]
    public async Task Nested_Tuples_Track_Shared_Elements_Once(((Fixture, int), Fixture) row)
        => await Check(row.Item2);

    private static async Task Check(Fixture fixture)
    {
        Received.Add(fixture);
        await Assert.That(fixture.Initializations).IsEqualTo(1);
        await Assert.That(fixture.Disposals).IsEqualTo(0);
    }

    [After(Class)]
    public static async Task VerifyDisposal()
    {
        foreach (var fixture in Received)
        {
            await Assert.That(fixture.Disposals).IsEqualTo(1);
        }
    }

    public sealed class Fixture : IAsyncInitializer, IAsyncDisposable
    {
        public int Initializations { get; private set; }
        public int Disposals { get; private set; }
        public Task InitializeAsync()
        {
            Initializations++;
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return default;
        }
    }
}
