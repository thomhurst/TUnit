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


[EngineTest(ExpectedResult.Pass)]
public class MutableTuplePropertyInjectionTests
{
    public static (Fixture, int) ValueRow() => (CreateFixture(), 1);
    public static Tuple<Fixture, int> ReferenceRow() => Tuple.Create(CreateFixture(), 1);
    public static Tuple<Fixture> SingleRow() => Tuple.Create(CreateFixture());
    public static (Fixture?, int) NullableRow() => (CreateFixture(), 1);
    public static Tuple<Fixture?> EmptyRow()
    {
        CreateFixture();
        return Tuple.Create<Fixture?>(null);
    }

    private static Fixture CreateFixture()
    {
        // Model a referenced type with no generated property metadata. Its injection
        // must use the reflection setter, which mutates the retained boxed struct.
        PropertySourceRegistry.Register(typeof(Fixture), new MissingPropertySource());
        return new Fixture();
    }

    [Test, MethodDataSource(nameof(ValueRow))]
    public async Task Value_Tuple_Retains_Property_Injection((Fixture, int) row)
        => await Assert.That(row.Item1.Dependency).IsNotNull();

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public async Task Reference_Tuple_Retains_Property_Injection(Tuple<Fixture, int> row)
        => await Assert.That(row.Item1.Dependency).IsNotNull();

    [Test, MethodDataSource(nameof(SingleRow))]
    public async Task Single_Tuple_Retains_Property_Injection(Tuple<Fixture> row)
        => await Assert.That(row.Item1.Dependency).IsNotNull();

    [Test, MethodDataSource(nameof(NullableRow))]
    public async Task Nullable_Tuple_Retains_Property_Injection((Fixture?, int) row)
        => await Assert.That(row.Item1!.Value.Dependency).IsNotNull();

    [Test, MethodDataSource(nameof(EmptyRow))]
    public async Task Empty_Nullable_Preserves_Tuple(Tuple<Fixture?> row)
    {
        await Assert.That(row).IsNotNull();
        await Assert.That(row.Item1).IsNull();
    }

    public struct Fixture
    {
        [ClassDataSource<Dependency>]
        public Dependency? Dependency { get; set; }
    }

    public sealed class Dependency;

    private sealed class MissingPropertySource : TUnit.Core.Interfaces.SourceGenerator.IPropertySource
    {
        public Type Type => typeof(Fixture);
        public bool ShouldInitialize => false;
        public IEnumerable<TUnit.Core.Interfaces.SourceGenerator.PropertyInjectionMetadata> GetPropertyMetadata() => [];
    }
}
