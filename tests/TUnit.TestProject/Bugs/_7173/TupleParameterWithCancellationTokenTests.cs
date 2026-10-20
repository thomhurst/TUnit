using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7173;

/// <summary>
/// A single tuple parameter receives the whole row, and the CancellationToken receives the test's
/// token, wherever the token is declared, in both source-generated and reflection mode (#7173).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class TupleParameterWithCancellationTokenTests
{
    public static IEnumerable<(int, int)> TupleParameterRows()
    {
        yield return (25, 26);
    }

    public static IEnumerable<(int, string)> SpreadRows()
    {
        yield return (1, "one");
    }

    public static IEnumerable<((int, int), int[])> TupleAndArrayRows()
    {
        yield return ((25, 26), [1, 2]);
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(TupleParameterRows))]
    public async Task Generic_Trailing_Token<T>((T, T) pair, CancellationToken token)
    {
        await Assert.That(pair).IsEqualTo(((T, T))(object)(25, 26));
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(TupleParameterRows))]
    public async Task Generic_Leading_Token<T>(CancellationToken token, (T, T) pair)
    {
        await Assert.That(pair).IsEqualTo(((T, T))(object)(25, 26));
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(TupleParameterRows))]
    public async Task NonGeneric_Trailing_Token((int, int) pair, CancellationToken token)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(TupleParameterRows))]
    public async Task NonGeneric_Leading_Token(CancellationToken token, (int, int) pair)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(SpreadRows))]
    public async Task NonGeneric_Middle_Token_Spreads_Row(int number, CancellationToken token, string name)
    {
        await Assert.That(number).IsEqualTo(1);
        await Assert.That(name).IsEqualTo("one");
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [Arguments(1, "one")]
    public async Task NonGeneric_Leading_Token_With_Arguments(CancellationToken token, int number, string name)
    {
        await Assert.That(number).IsEqualTo(1);
        await Assert.That(name).IsEqualTo("one");
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [Arguments]
    public async Task NonGeneric_Token_Before_Params(int count = 42, CancellationToken token = default, params int[] values)
    {
        await Assert.That(count).IsEqualTo(42);
        await Assert.That(values).IsEmpty();
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [Arguments(1, 2, 3)]
    public async Task NonGeneric_Token_Before_Params_With_Values(int count, CancellationToken token, params int[] values)
    {
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(values).IsEquivalentTo([2, 3]);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    // Every CancellationToken parameter receives the test's token; the row binds to the others.

    [Test]
    public async Task NonGeneric_Two_Tokens_No_Data(CancellationToken first, CancellationToken second)
    {
        await Assert.That(first.CanBeCanceled).IsTrue();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    [Arguments(7)]
    public async Task NonGeneric_Tokens_Around_Data(CancellationToken first, int number, CancellationToken second)
    {
        await Assert.That(number).IsEqualTo(7);
        await Assert.That(first.CanBeCanceled).IsTrue();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    [MethodDataSource(nameof(TupleParameterRows))]
    public async Task NonGeneric_Tokens_Around_Tuple(CancellationToken first, (int, int) pair, CancellationToken second)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(first.CanBeCanceled).IsTrue();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    [Arguments(1, 2, 3)]
    public async Task NonGeneric_Array_Before_Two_Tokens(int[] values, CancellationToken first, CancellationToken second)
    {
        await Assert.That(values).IsEquivalentTo([1, 2, 3]);
        await Assert.That(first.CanBeCanceled).IsTrue();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(TupleParameterRows))]
    public async Task Generic_Tokens_Around_Tuple<T>(CancellationToken first, (T, T) pair, CancellationToken second)
    {
        await Assert.That(pair).IsEqualTo(((T, T))(object)(25, 26));
        await Assert.That(first.CanBeCanceled).IsTrue();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    [MethodDataSource(nameof(SpreadRows))]
    public async Task NonGeneric_Tokens_Interleaved_With_Spread_Row(CancellationToken first, int number, CancellationToken second, string name)
    {
        await Assert.That(number).IsEqualTo(1);
        await Assert.That(name).IsEqualTo("one");
        await Assert.That(first.CanBeCanceled).IsTrue();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    [GenerateGenericTest(typeof(int))]
    [MethodDataSource(nameof(TupleAndArrayRows))]
    public async Task Generic_Tuple_Middle_Token_Params<T>((T, T) pair, CancellationToken token, params int[] values)
    {
        await Assert.That(pair).IsEqualTo(((T, T))(object)(25, 26));
        await Assert.That(values).IsEquivalentTo([1, 2]);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }
}
