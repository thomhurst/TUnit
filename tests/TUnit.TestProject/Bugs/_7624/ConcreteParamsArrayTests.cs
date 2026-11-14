using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7624;

[EngineTest(ExpectedResult.Pass)]
public class ConcreteParamsArrayTests
{
    public static IEnumerable<((int, string), int)> ExpandedValueRows() => [((1, "tuple"), 7)];
    public static IEnumerable<(Tuple<int, string>, int)> ExpandedReferenceRows() => [(Tuple.Create(1, "tuple"), 7)];
    // Each enumeration creates fresh arrays; direct array columns exercise normal-form binding.
#pragma warning disable TUnit0046
    public static IEnumerable<((int, string), int[])> ExplicitValueRows() => [((1, "tuple"), new[] { 7, 8 })];
    public static IEnumerable<(Tuple<int, string>, int[])> ExplicitReferenceRows() => [(Tuple.Create(1, "tuple"), new[] { 7, 8 })];
#pragma warning restore TUnit0046

    // The analyzer's nested-row diagnostic is tracked separately by #7622.
#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(ExpandedValueRows))]
    public async Task Expanded_Value_Tuple<T>((T, string) row, params int[] extra) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item2).IsEqualTo("tuple");
        await Assert.That(extra.SequenceEqual(new[] { 7 })).IsTrue();
    }

    [Test, MethodDataSource(nameof(ExpandedReferenceRows))]
    public async Task Expanded_Reference_Tuple<T>(Tuple<T, string> row, params int[] extra) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item2).IsEqualTo("tuple");
        await Assert.That(extra.SequenceEqual(new[] { 7 })).IsTrue();
    }

    [Test, MethodDataSource(nameof(ExplicitValueRows))]
    public async Task Explicit_Value_Tuple<T>((T, string) row, params int[] extra) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item2).IsEqualTo("tuple");
        await Assert.That(extra.SequenceEqual(new[] { 7, 8 })).IsTrue();
    }

    [Test, MethodDataSource(nameof(ExplicitReferenceRows))]
    public async Task Explicit_Reference_Tuple<T>(Tuple<T, string> row, params int[] extra) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item2).IsEqualTo("tuple");
        await Assert.That(extra.SequenceEqual(new[] { 7, 8 })).IsTrue();
    }
#pragma warning restore TUnit0001

    [Test, Arguments(1, 7, 8)]
    public async Task Multiple_Expanded_Values<T>(T first, params int[] extra) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(extra.SequenceEqual(new[] { 7, 8 })).IsTrue();
    }

    [Test, Arguments(1, 7)]
    public async Task CancellationToken_Before_Params<T>(T first, CancellationToken token, params int[] extra) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(token.CanBeCanceled).IsTrue();
        await Assert.That(extra.SequenceEqual(new[] { 7 })).IsTrue();
    }
}
