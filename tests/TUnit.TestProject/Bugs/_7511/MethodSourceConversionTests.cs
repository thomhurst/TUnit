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

public sealed class InheritedClassSourceValue
{
    public static implicit operator int(InheritedClassSourceValue value) => 48;
}

[MethodDataSource(typeof(InheritedClassSourceBase), nameof(InheritedClassSourceBase.Rows))]
public abstract class InheritedClassSourceBase
{
    public static InheritedClassSourceValue Rows() => new();
}

public class InheritedClassSourceTests(int value) : InheritedClassSourceBase
{
    [Test]
    public async Task ConvertsInheritedSource() => await Assert.That(value).IsEqualTo(48);
}

public sealed class PropertySourceValue
{
    public static implicit operator int(PropertySourceValue value) => 49;
}

public abstract class PropertySourceBase
{
    // The source is supplied by the derived test class; analyzer support is tracked in #7551.
#pragma warning disable TUnit0004
    [MethodDataSource("Rows")]
#pragma warning restore TUnit0004
    public int Value { get; set; }
}

public class PropertySourceConversionTests : PropertySourceBase
{
    public static PropertySourceValue Rows() => new();

    [Test]
    public async Task ConvertsInheritedProperty() => await Assert.That(Value).IsEqualTo(49);
}

public class DecimalSourceConversionTests
{
    [Test]
    public async Task DecimalOverflowThrows()
    {
        await Assert.That(() => TUnit.Core.Helpers.CastHelper.Cast<int>(decimal.MaxValue))
            .Throws<OverflowException>();
    }

    public static (decimal, int)[] Rows() => [(1.9m, 2), (2.5m, 2), (3.5m, 4)];

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task ConvertsToSbyte(sbyte value, int expected)
    {
        await Assert.That((long)value).IsEqualTo(expected);
    }

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task ConvertsToByte(byte value, int expected)
    {
        await Assert.That((long)value).IsEqualTo(expected);
    }

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task ConvertsToShort(short value, int expected)
    {
        await Assert.That((long)value).IsEqualTo(expected);
    }

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task ConvertsToUshort(ushort value, int expected)
    {
        await Assert.That((long)value).IsEqualTo(expected);
    }

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task ConvertsToInt(int value, int expected)
    {
        await Assert.That((long)value).IsEqualTo(expected);
    }

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task ConvertsToUint(uint value, int expected)
    {
        await Assert.That((long)value).IsEqualTo(expected);
    }

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task ConvertsToLong(long value, int expected)
    {
        await Assert.That((long)value).IsEqualTo(expected);
    }

#pragma warning disable TUnit0001
    [Test, MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task ConvertsToUlong(ulong value, int expected)
    {
        await Assert.That((long)value).IsEqualTo(expected);
    }

}
