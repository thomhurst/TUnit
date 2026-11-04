namespace TUnit.TestProject.Bugs._7511;

public sealed class SourceValue
{
    public static implicit operator int(SourceValue value) => 42;
}

public sealed class ExplicitSourceValue
{
    public static explicit operator int(ExplicitSourceValue value) => 43;
}

public sealed class TupleSourceValue
{
    public static implicit operator (int, int)(TupleSourceValue value) => (42, 43);
}

public sealed class ExplicitTupleSourceValue
{
    public static explicit operator (int, int)(ExplicitTupleSourceValue value) => (44, 45);
}

public sealed class ConcreteValues : List<SourceValue>;

public class MethodSourceConversionTests
{
    public static ConcreteValues Concrete() => [new()];
    public static SourceValue Scalar() => new();
    public static ExplicitSourceValue Explicit() => new();
    public static (TupleSourceValue, int) Tuple() => (new(), 46);
    public static (ExplicitTupleSourceValue, int) ExplicitTuple() => (new(), 47);
    public static SourceValue[] Array() => [new()];
    public static IEnumerable<SourceValue> Enumerable() => [new()];
    public static Task<SourceValue> Async() => Task.FromResult(new SourceValue());
    public static ValueTask<SourceValue> ValueAsync() => new(new SourceValue());
    public static Task<IEnumerable<SourceValue>> AsyncEnumerable() => Task.FromResult<IEnumerable<SourceValue>>([new()]);
    public static async IAsyncEnumerable<SourceValue> AsyncRows()
    {
        await Task.Yield();
        yield return new();
    }

    [Test, MethodDataSource(nameof(Scalar))]
    [MethodDataSource(nameof(Concrete))]
    [MethodDataSource(nameof(Array))]
    [MethodDataSource(nameof(Enumerable))]
    [MethodDataSource(nameof(Async))]
    [MethodDataSource(nameof(ValueAsync))]
    [MethodDataSource(nameof(AsyncEnumerable))]
    [MethodDataSource(nameof(AsyncRows))]
    public async Task Converts(int value) => await Assert.That(value).IsEqualTo(42);

    // Explicit conversions are supported at runtime; analyzer policy currently requires implicit conversion.
#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Explicit))]
#pragma warning restore TUnit0001
    public async Task ConvertsExplicit(int value) => await Assert.That(value).IsEqualTo(43);

    [Test, MethodDataSource(nameof(Tuple))]
    public async Task ConvertsTuple((int, int) value, int other)
    {
        await Assert.That(value).IsEqualTo((42, 43));
        await Assert.That(other).IsEqualTo(46);
    }

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(ExplicitTuple))]
#pragma warning restore TUnit0001
    public async Task ConvertsExplicitTuple((int, int) value, int other)
    {
        await Assert.That(value).IsEqualTo((44, 45));
        await Assert.That(other).IsEqualTo(47);
    }
}
