using TUnit.TestProject.Attributes;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class GenericMutableTupleInitializerTests
{
    public static (Fixture, int) ValueRow() => (new(), 1);
    public static Tuple<Fixture, int> ReferenceRow() => Tuple.Create(new Fixture(), 1);
    public static Tuple<Fixture> SingleRow() => Tuple.Create(new Fixture());

    [Test, MethodDataSource(nameof(SingleRow))]
    public async Task Single_Tuple_Infers_Generic_Element<T>(Tuple<T> row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(Fixture));
        await Assert.That(((Fixture)(object)row.Item1!).Initialized).IsTrue();
    }

    public static (Fixture, int, int, int, int, int, int, string) LongRow() => (new(), 1, 2, 3, 4, 5, 6, "last");

    [Test, MethodDataSource(nameof(LongRow))]
    public async Task Long_Tuple_Infers_Generic_Element<T>((Fixture, int, int, int, int, int, int, T) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(row.Item1.Initialized).IsTrue();
        await Assert.That((object?)row.Item8).IsEqualTo("last");
    }

    [Test, MethodDataSource(nameof(ValueRow))]
    public async Task Tuple_Infers_Generic_Element_With_Leading_Token<T>(CancellationToken token, (Fixture, T) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item1.Initialized).IsTrue();
        await Assert.That(token.IsCancellationRequested).IsFalse();
    }

    [Test, MethodDataSource(nameof(ValueRow))]
    public async Task Value_Tuple_Infers_Generic_Element<T>((Fixture, T) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item1.Initialized).IsTrue();
        await Assert.That((object?)row.Item2).IsEqualTo(1);
    }

    [Test, MethodDataSource(nameof(ReferenceRow))]
    public async Task Reference_Tuple_Infers_Generic_Element<T>(Tuple<Fixture, T> row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item1.Initialized).IsTrue();
        await Assert.That((object?)row.Item2).IsEqualTo(1);
    }
    public static (Fixture, CancellationToken, int) TokenRow() => (new(), default, 1);

    [Test, MethodDataSource(nameof(TokenRow))]
    public async Task Tuple_Token_Element_Binds_As_Data<T>((Fixture, CancellationToken, T) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item1.Initialized).IsTrue();
        await Assert.That((object?)row.Item3).IsEqualTo(1);
    }

    public static (object?, object?, Fixture) NullRow() => (1, null, new());
    public static Tuple<object?, object?, Fixture> NullReferenceRow() => Tuple.Create<object?, object?, Fixture>(1, null, new());

    [Test, MethodDataSource(nameof(NullRow))]
    public async Task Null_Element_Keeps_Generic_Reference_Type<T>((T, T, Fixture) row)
        => await CheckNullElement(row.Item1, row.Item2, row.Item3);

    [Test, MethodDataSource(nameof(NullReferenceRow))]
    public async Task Reference_Null_Element_Keeps_Generic_Reference_Type<T>(Tuple<T, T, Fixture> row)
        => await CheckNullElement(row.Item1, row.Item2, row.Item3);

    [Test, MethodDataSource(nameof(NullRow))]
    public async Task Null_Element_With_Leading_Token<T>(CancellationToken token, (T, T, Fixture) row)
    {
        await CheckNullElement(row.Item1, row.Item2, row.Item3);
        await Assert.That(token.IsCancellationRequested).IsFalse();
    }

    [Test, MethodDataSource(nameof(NullRow))]
    public async Task Null_Element_With_Trailing_Token<T>((T, T, Fixture) row, CancellationToken token)
    {
        await CheckNullElement(row.Item1, row.Item2, row.Item3);
        await Assert.That(token.IsCancellationRequested).IsFalse();
    }

    private static async Task CheckNullElement<T>(T first, T second, Fixture fixture)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That((object?)first).IsEqualTo(1);
        await Assert.That((object?)second).IsNull();
        await Assert.That(fixture.Initialized).IsTrue();
    }

    public struct Fixture : IAsyncInitializer
    {
        public bool Initialized;
        public Task InitializeAsync()
        {
            Initialized = true;
            return Task.CompletedTask;
        }
    }
}
