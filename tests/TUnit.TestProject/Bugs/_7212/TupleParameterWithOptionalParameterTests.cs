using TUnit.TestProject.Attributes;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._7212;

/// <summary>
/// A row whose single value is a tuple of the first data parameter's type binds to that parameter
/// whole, and the remaining optional or params parameters take their defaults, in both
/// source-generated and reflection mode, for object[] and typed sources (#7212, #7340).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class TupleParameterWithOptionalParameterTests
{
    public static IEnumerable<object[]> TupleRows()
    {
        yield return [(25, 26)];
    }

    public static IEnumerable<(int, int)> TypedTupleRows()
    {
        yield return (25, 26);
    }

    public static IEnumerable<TestDataRow<(int, int)>> WrappedTupleRows()
    {
        yield return new TestDataRow<(int, int)>((25, 26));
    }

    public static Task<IEnumerable<(int, int)>> TaskTupleRows() => Task.FromResult(TypedTupleRows());

    public static ValueTask<IEnumerable<(int, int)>> ValueTaskTupleRows() => new(TypedTupleRows());

    public static async IAsyncEnumerable<(int, int)> AsyncTupleRows()
    {
        await Task.Yield();
        yield return (25, 26);
    }

    public IEnumerable<(int, int)> InstanceTupleRows() => TypedTupleRows();

    public static IEnumerable<object[]> TupleAndValueRows()
    {
        yield return [(25, 26), 7];
    }

    public static IEnumerable<((int, int), int)> NestedTupleRows()
    {
        yield return ((25, 26), 7);
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    [MethodDataSource(nameof(WrappedTupleRows))]
    [MethodDataSource(nameof(TaskTupleRows))]
    [MethodDataSource(nameof(ValueTaskTupleRows))]
    [MethodDataSource(nameof(AsyncTupleRows))]
    [MethodDataSource(nameof(InstanceTupleRows))]
    public async Task Tuple_And_Optional((int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    [MethodDataSource(nameof(WrappedTupleRows))]
    public async Task Tuple_And_Two_Optionals((int, int) pair, int extra = 5, string name = "default")
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(name).IsEqualTo("default");
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    [MethodDataSource(nameof(WrappedTupleRows))]
    public async Task Tuple_Optional_And_Token((int, int) pair, int extra = 5, CancellationToken token = default)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    [MethodDataSource(nameof(WrappedTupleRows))]
    public async Task Token_Tuple_And_Optional(CancellationToken token, (int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    [MethodDataSource(nameof(WrappedTupleRows))]
    public async Task Tuple_And_Params((int, int) pair, params int[] rest)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(rest).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    [MethodDataSource(nameof(WrappedTupleRows))]
    public async Task Tuple_And_Params_Collection((int, int) pair, params IEnumerable<int> rest)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(rest).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(TupleAndValueRows))]
    public async Task Tuple_And_Supplied_Optional((int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(7);
    }

    [Test]
    [MethodDataSource(nameof(NestedTupleRows))]
    public async Task Nested_Tuple_Row_Is_Spread((int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(7);
    }
}


[EngineTest(ExpectedResult.Pass)]
public class MutableTupleWithOptionalParameterTests
{
    public static Func<(Fixture, int)> ValueRow() => () => (CreateFixture(), 26);
    private static Fixture CreateFixture()
    {
        // #7609 tracks generated struct setters copying the box. Exercise the reflection
        // property setter here, as MutableTuplePropertyInjectionTests does.
        PropertySourceRegistry.Register(typeof(Fixture), new MissingPropertySource());
        return new Fixture();
    }
    public static IEnumerable<TestDataRow<Func<(Fixture, int)>>> WrappedRows()
    {
        yield return new TestDataRow<Func<(Fixture, int)>>(ValueRow());
    }
    public static IEnumerable<object[]> ArrayRows()
    {
        yield return [(CreateFixture(), 26)];
    }
    public static Tuple<Fixture, int> ReferenceRow() => Tuple.Create(CreateFixture(), 26);

    [Test, MethodDataSource(nameof(ValueRow)), MethodDataSource(nameof(WrappedRows)), MethodDataSource(nameof(ArrayRows))]
    public async Task Mutable_Tuple_And_Optional((Fixture, int) pair, int extra = 5)
    {
        await Check(pair.Item1, pair.Item2);
        await Assert.That(extra).IsEqualTo(5);
    }

    [Test, MethodDataSource(nameof(ValueRow)), MethodDataSource(nameof(WrappedRows))]
    public async Task Mutable_Tuple_And_Params((Fixture, int) pair, params int[] rest)
    {
        await Check(pair.Item1, pair.Item2);
        await Assert.That(rest).IsEmpty();
    }

    [Test, MethodDataSource(nameof(ValueRow))]
    public async Task Token_Mutable_Tuple_And_Optional(CancellationToken token, (Fixture, int) pair, int extra = 5)
    {
        await Check(pair.Item1, pair.Item2);
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test, MethodDataSource(nameof(ValueRow))]
    public async Task Mutable_Tuple_Optional_And_Token((Fixture, int) pair, int extra = 5, CancellationToken token = default)
    {
        await Check(pair.Item1, pair.Item2);
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public async Task Mutable_Reference_Tuple_And_Optional(Tuple<Fixture, int> pair, int extra = 5)
    {
        await Check(pair.Item1, pair.Item2);
        await Assert.That(extra).IsEqualTo(5);
    }

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public async Task Mutable_Reference_Tuple_And_Params(Tuple<Fixture, int> pair, params int[] rest)
    {
        await Check(pair.Item1, pair.Item2);
        await Assert.That(rest).IsEmpty();
    }

    public static Func<((Fixture, int), int)> NestedValueRow() => () => ((CreateFixture(), 26), 27);
    public static Func<Tuple<(Fixture, int), int>> NestedReferenceRow() => () => Tuple.Create((CreateFixture(), 26), 27);

    [Test, MethodDataSource(nameof(NestedValueRow))]
    public async Task Nested_Mutable_Tuple_And_Optional(((Fixture, int), int) pair, int extra = 5)
    {
        await Check(pair.Item1.Item1, pair.Item1.Item2);
        await Assert.That(pair.Item2).IsEqualTo(27);
        await Assert.That(extra).IsEqualTo(5);
    }

    [Test, MethodDataSource(nameof(NestedReferenceRow))]
    public async Task Nested_Mutable_Reference_Tuple_And_Optional(Tuple<(Fixture, int), int> pair, int extra = 5)
    {
        await Check(pair.Item1.Item1, pair.Item1.Item2);
        await Assert.That(pair.Item2).IsEqualTo(27);
        await Assert.That(extra).IsEqualTo(5);
    }

    public static Func<(Tuple<Fixture, int>, int)> NestedInnerReferenceRow() => () => (Tuple.Create(CreateFixture(), 26), 27);

    [Test, MethodDataSource(nameof(NestedInnerReferenceRow))]
    public async Task Nested_Inner_Reference_Tuple_And_Optional((Tuple<Fixture, int>, int) pair, int extra = 5)
    {
        await Check(pair.Item1.Item1, pair.Item1.Item2);
        await Assert.That(pair.Item2).IsEqualTo(27);
        await Assert.That(extra).IsEqualTo(5);
    }

    private static async Task Check(Fixture fixture, int number)
    {
        await Assert.That(fixture.Initialized).IsTrue();
        await Assert.That(fixture.Started).IsTrue();
        await Assert.That(fixture.Dependency).IsNotNull();
        await Assert.That(number).IsEqualTo(26);
    }

    public struct Fixture : IAsyncInitializer, ITestStartEventReceiver
    {
        public bool Initialized;
        public bool Started;
        public int Order => 0;
        [ClassDataSource<Dependency>]
        public Dependency? Dependency { get; set; }
        public Task InitializeAsync()
        {
            Initialized = true;
            return Task.CompletedTask;
        }
        public ValueTask OnTestStart(TestContext context)
        {
            Started = true;
            return default;
        }
    }
    public sealed class Dependency;
    private sealed class MissingPropertySource : TUnit.Core.Interfaces.SourceGenerator.IPropertySource
    {
        public Type Type => typeof(Fixture);
        public bool ShouldInitialize => false;
        public IEnumerable<TUnit.Core.Interfaces.SourceGenerator.PropertyInjectionMetadata> GetPropertyMetadata() => [];
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ConvertedAndGenericTupleOptionalTests
{
    public static IEnumerable<object[]> ConvertedRows() => [[new ConvertibleList()]];
    public static IEnumerable<object[]> GenericRows() => [[(25, 26)]];

    [Test, MethodDataSource(nameof(ConvertedRows))]
    public async Task Convertible_List_And_Optional((int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
    }

    [Test, GenerateGenericTest(typeof(int)), MethodDataSource(nameof(GenericRows))]
    public async Task Generic_Tuple_And_Optional<T>((T, int) pair, int extra = 5)
    {
        await Assert.That(pair.Item1).IsEqualTo((T)(object)25);
        await Assert.That(pair.Item2).IsEqualTo(26);
        await Assert.That(extra).IsEqualTo(5);
    }

    public sealed class ConvertibleList : List<object>
    {
        public static implicit operator (int, int)(ConvertibleList value) => (25, 26);
    }
}
