using System.Runtime.CompilerServices;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5879;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/5879.
///
/// `[InheritsTests]` derived class extends a generic abstract base whose
/// `[MethodDataSource]` method has an optional `[EnumeratorCancellation] CancellationToken ct = default`
/// parameter. Source generation can't emit a `Factory` for this case, so the engine
/// falls back to <c>MethodDataSourceAttribute.GetDataRowsAsync</c>'s reflection
/// invoke path. <c>MethodInfo.Invoke</c> does not auto-fill defaults like a C# call
/// site does, so before the fix the call threw <c>TargetParameterCountException</c>.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class Issue5879Tests : Issue5879AbstractBase<string, string>
{
    protected override (string, string) Value => ("test", "test");
}

public abstract class Issue5879AbstractBase<TInput, TExpected>
{
    [Test]
    [MethodDataSource(nameof(DataSource))]
    public async ValueTask Reproduce((TInput, TExpected) value)
    {
        var (input, expected) = value;
        await Assert.That(input).IsEquivalentTo(expected);
    }

    public async IAsyncEnumerable<(TInput, TExpected)> DataSource(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return Value;
        await Task.CompletedTask;
    }

    protected abstract (TInput, TExpected) Value { get; }
}
