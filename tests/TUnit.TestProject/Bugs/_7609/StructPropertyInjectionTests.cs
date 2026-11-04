using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7609;

[EngineTest(ExpectedResult.Pass)]
public class StructPropertyInjectionTests
{
    // A concrete property makes the generic struct available to property metadata discovery.
    public GenericFixture<Dependency> GenericMetadata { get; set; }

    public static Func<InitFixture> InitRow() => () => new InitFixture();
    public static Func<Fixture> Row() => () => new Fixture();
    public static Func<GenericFixture<Dependency>> GenericRow() => () => new GenericFixture<Dependency>();
    public static (Fixture, int) ValueRow() => (new Fixture(), 1);
    public static Tuple<Fixture, int> ReferenceRow() => Tuple.Create(new Fixture(), 1);

    [Test, MethodDataSource(nameof(Row))]
    public async Task Ordinary_Argument(Fixture fixture) => await Check(fixture);

    [Test, MethodDataSource(nameof(ValueRow))]
    public async Task Value_Tuple((Fixture, int) row) => await Check(row.Item1);

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public async Task Reference_Tuple(Tuple<Fixture, int> row) => await Check(row.Item1);

    [Test, MethodDataSource(nameof(InitRow))]
    public async Task Init_Only_Argument(InitFixture fixture)
        => await Assert.That(fixture.InitDependency).IsNotNull();

    [Test, MethodDataSource(nameof(GenericRow))]
    public async Task Generic_Argument(GenericFixture<Dependency> fixture)
    {
        await Assert.That(fixture.Dependency).IsNotNull();
        await Assert.That(fixture.InitDependency).IsNotNull();
    }

    private static async Task Check(Fixture fixture)
        => await Assert.That(fixture.Dependency).IsNotNull();

    public struct Fixture
    {
        [ClassDataSource<Dependency>]
        public Dependency? Dependency { get; set; }
    }

    public struct InitFixture
    {
        [ClassDataSource<Dependency>]
        public Dependency? InitDependency { get; init; }
    }

    public struct GenericFixture<T> where T : class, new()
    {
        [ClassDataSource<Dependency>]
        public Dependency? Dependency { get; set; }

        [ClassDataSource<Dependency>]
        public Dependency? InitDependency { get; init; }
    }

    public sealed class Dependency;
}
