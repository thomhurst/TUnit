using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7173;

/// <summary>
/// Generic type arguments are inferred from the data parameters only: a CancellationToken parameter
/// does not consume a row position, in both source-generated and reflection mode (#7173).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericClassInferenceWithCancellationTokenTests<T>
{
    [Test]
    [Arguments(1)]
    public async Task Arguments_Leading_Token(CancellationToken token, T value)
    {
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [Arguments(1, "one")]
    public async Task Arguments_Tokens_Around_Generic(CancellationToken first, T value, CancellationToken second, string name)
    {
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(name).IsEqualTo("one");
        await Assert.That(second).IsEqualTo(first);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenericMethodInferenceWithCancellationTokenTests
{
    public static IEnumerable<Func<(int, string)>> FuncTupleRows()
    {
        yield return () => (1, "one");
    }

    [Test]
    [MethodDataSource(nameof(FuncTupleRows))]
    public async Task Func_Tuple_Row_Leading_Token<T>(CancellationToken token, T value, string name)
    {
        await Assert.That(value).IsEqualTo((T)(object)1);
        await Assert.That(name).IsEqualTo("one");
        await Assert.That(token.CanBeCanceled).IsTrue();
    }
}
