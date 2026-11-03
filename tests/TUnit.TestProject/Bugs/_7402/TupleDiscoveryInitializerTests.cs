using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class TupleDiscoveryInitializerTests
{
    public static (Fixture, int) ValueRow() => (new Fixture(), 1);
    public static Tuple<Fixture, int> ReferenceRow() => Tuple.Create(new Fixture(), 1);
    public static ((Fixture, int), Fixture) NestedRow()
    {
        var fixture = new Fixture();
        return ((fixture, 1), fixture);
    }

    [Test, MethodDataSource(nameof(ValueRow))]
    public async Task Value_Tuple_Initializes_Before_Registration((Fixture, int) row)
        => await Check(row.Item1);

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public async Task Reference_Tuple_Initializes_Before_Registration(Tuple<Fixture, int> row)
        => await Check(row.Item1);

    [Test, MethodDataSource(nameof(NestedRow))]
    public async Task Nested_Tuple_Initializes_Shared_Element_Once(((Fixture, int), Fixture) row)
        => await Check(row.Item2);

    private static async Task Check(Fixture fixture)
    {
        await Assert.That(fixture.InitializationsAtRegistration).IsEqualTo(1);
        await Assert.That(fixture.Initializations).IsEqualTo(1);
    }

    public sealed class Fixture : IAsyncDiscoveryInitializer, ITestRegisteredEventReceiver
    {
        public int Initializations { get; private set; }
        public int InitializationsAtRegistration { get; private set; }
        public int Order => 0;

        public Task InitializeAsync()
        {
            Initializations++;
            return Task.CompletedTask;
        }

        public ValueTask OnTestRegistered(TestRegisteredContext context)
        {
            InitializationsAtRegistration = Initializations;
            return default;
        }
    }
}
