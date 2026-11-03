using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class TuplePropertyBindingTests
{
    public static Tuple<Fixture> ReferenceRow() => Tuple.Create(new Fixture(42));
    public static ValueTuple<Fixture> ValueRow() => new(new Fixture(43));

    [MethodDataSource(nameof(ReferenceRow))]
    public required Tuple<Fixture> ReferenceProperty { get; init; }

    [MethodDataSource(nameof(ValueRow))]
    public required ValueTuple<Fixture> ValueProperty { get; init; }

    [Test]
    public async Task Struct_Initializer_Tuples_Keep_Property_Shape()
    {
        await Assert.That(ReferenceProperty.Item1.Value).IsEqualTo(42);
        await Assert.That(ValueProperty.Item1.Value).IsEqualTo(43);
    }

    public readonly record struct Fixture(int Value) : IAsyncInitializer
    {
        public Task InitializeAsync() => Task.CompletedTask;
    }
}
