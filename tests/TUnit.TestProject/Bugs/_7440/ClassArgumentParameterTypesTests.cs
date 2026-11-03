using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7440;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[IntRows7440]
public class ClassArgumentParameterTypesTests<T>(T value)
{
    [Test]
    [Arguments(2)]
    public async Task Run(decimal number)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value is int actual && (actual == 1 || actual == 7)).IsTrue();
        await Assert.That(number).IsEqualTo(2m);
    }
}

public class IntRows7440Attribute : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
        => [() => 7];
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(3)]
[Arguments(3L)]
[Arguments(int.MaxValue)]
public class DecimalConstructorArgumentTests(decimal value)
{
    [Test]
    [Arguments(4)]
    public async Task Run(int number)
    {
        await Assert.That(value == 3m || value == int.MaxValue).IsTrue();
        await Assert.That(number).IsEqualTo(4);
    }
}

public static class DecimalArgumentConstants
{
    public const double DoubleRate = 0.1;
    public const float FloatRate = 0.1f;
    public const string Amount = "3\n";
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(DecimalArgumentConstants.DoubleRate)]
[Arguments(DecimalArgumentConstants.FloatRate)]
public class DecimalConstantConstructorArgumentTests(decimal value)
{
    [Test]
    [Arguments(DecimalArgumentConstants.DoubleRate)]
    [Arguments(DecimalArgumentConstants.FloatRate)]
    public async Task Run(decimal number)
    {
        await Assert.That(value).IsEqualTo(0.1m);
        await Assert.That(number).IsEqualTo(0.1m);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(DecimalArgumentConstants.Amount)]
public class DecimalStringConstantConstructorArgumentTests(decimal value)
{
    [Test]
    [Arguments(DecimalArgumentConstants.Amount)]
    public async Task Run(decimal number)
    {
        await Assert.That(value).IsEqualTo(3m);
        await Assert.That(number).IsEqualTo(3m);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(3L, 1)]
[Arguments(4L, 2)]
[Arguments(DecimalArgumentConstants.DoubleRate, 3)]
[Arguments(DecimalArgumentConstants.FloatRate, 4)]
[Arguments(DecimalArgumentConstants.Amount, 5)]
public class GenericDecimalConstructorArgumentTests<T>(decimal amount, T value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        var row = (int)(object)value!;
        await Assert.That(amount).IsEqualTo(row switch
        {
            1 => 3m,
            2 => 4m,
            3 or 4 => 0.1m,
            _ => 3m
        });
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments("first", 1)]
[Arguments("second", 2)]
public class RequiredConstructorArgumentTests<T>
{
    public required T Value { get; init; }
    public required int Number { get; init; }

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public RequiredConstructorArgumentTests(T value, int number)
    {
        Value = value;
        Number = number;
    }

    [Test]
    public async Task Run()
    {
        await Assert.That(Value is not null).IsTrue();
        await Assert.That(Number).IsEqualTo((string)(object)Value! == "first" ? 1 : 2);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments("empty")]
[Arguments("single", 1)]
[Arguments("multiple", 1, 2)]
[Arguments("array", new int[] { 1, 2 })]
public class ParamsConstructorArgumentTests<T>(T value, params int[] rest)
{
    [Test]
    public async Task Run()
    {
        var expectedLength = (string)(object)value! switch
        {
            "empty" => 0,
            "single" => 1,
            _ => 2
        };
        await Assert.That(rest.Length).IsEqualTo(expectedLength);
        for (var i = 0; i < rest.Length; i++)
        {
            await Assert.That(rest[i]).IsEqualTo(i + 1);
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
public class OptionalArrayConstructorArgumentTests<T>(T value, int[]? rest = null)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(value is int).IsTrue();
        await Assert.That(rest).IsNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments]
public class OptionalOnlyArrayConstructorArgumentTests(int[]? rest = null)
{
    [Test]
    public async Task Run() => await Assert.That(rest).IsNull();
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1, "first")]
[Arguments(2, "second")]
public class SelectedConstructorArgumentTests<T>
{
    private readonly T _value;
    private readonly string _text;

    public SelectedConstructorArgumentTests(T value) => throw new InvalidOperationException("Unselected constructor");

    [TestConstructor]
    public SelectedConstructorArgumentTests(T value, string text)
    {
        _value = value;
        _text = text;
    }

    [Test]
    public async Task Run()
    {
        await Assert.That((int)(object)_value!).IsEqualTo(_text == "first" ? 1 : 2);
    }
}

public sealed class ConstructorArrayWrapper
{
    public static implicit operator int[](ConstructorArrayWrapper value) => [1, 2];
}

public sealed class ConstructorArrayRowsAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        // Untyped sources hide their runtime types from converter generation. Register the
        // conversion for Native AOT; JIT runs exercise reflection operator discovery instead.
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
        {
            TUnit.Core.Converters.AotConverterRegistry.Register<ConstructorArrayWrapper, int[]>(value => value);
        }

        return [() => [new ConstructorArrayWrapper()]];
    }
}

[EngineTest(ExpectedResult.Pass)]
[ConstructorArrayRows]
public class ConvertedArrayConstructorArgumentTests(int[] values)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That(values[0]).IsEqualTo(1);
        await Assert.That(values[1]).IsEqualTo(2);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1.2345678901234567, 1)]
[Arguments(1.2345678901234567d, 2)]
[Arguments(1.23456789f, 3)]
public class FloatingDecimalConstructorArgumentTests<T>(decimal value, T row)
{
    [Test]
    public async Task Run()
    {
        var expected = (int)(object)row! == 3
            ? Convert.ToDecimal(1.23456789f)
            : Convert.ToDecimal(1.2345678901234567d);
        await Assert.That(value).IsEqualTo(expected);
    }
}

public static class OverloadedConstructorRows7440
{
    public static IEnumerable<int> Values() => [5];
}

#pragma warning disable TUnit0052 // Pin existing first-public constructor selection without TestConstructor.
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(OverloadedConstructorRows7440), nameof(OverloadedConstructorRows7440.Values))]
public class RuntimeRowOverloadedConstructorTests<T>
{
    private readonly T _value;
    public RuntimeRowOverloadedConstructorTests(T value) => _value = value;
    public RuntimeRowOverloadedConstructorTests(T value, string other) => throw new InvalidOperationException("Unselected constructor");

    [Test]
    [Arguments(1)]
    public async Task Run(T argument) => await Assert.That(_value!.ToString()).IsEqualTo("5");
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(5)]
[GenerateGenericTest(typeof(string))]
public class InferredRowOverloadedConstructorTests<T>
{
    private readonly T _value;
    public InferredRowOverloadedConstructorTests(T value) => _value = value;
    public InferredRowOverloadedConstructorTests(T value, string other) => throw new InvalidOperationException("Unselected constructor");

    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Run(T argument) => await Assert.That(_value!.ToString()).IsEqualTo("5");
}
#pragma warning restore TUnit0052

[EngineTest(ExpectedResult.Pass)]
[Arguments<int>(3)]
public class GenericAttributeDecimalConstructorTests(decimal value)
{
    [Test]
    public async Task Run() => await Assert.That(value).IsEqualTo(3m);
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1, 42)]
public class SingleGenericDecimalConstructorTests<T>(decimal amount, T value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(amount).IsEqualTo(1m);
        await Assert.That((int)(object)value!).IsEqualTo(42);
    }
}

public abstract class RequiredFieldBase
{
    public required string? Inherited;
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class SingleGenericRequiredFieldTests<T>(T value) : RequiredFieldBase
{
    public required int Number;

    [Test]
    public async Task Run()
    {
        await Assert.That((int)(object)value!).IsEqualTo(1);
        await Assert.That(Number).IsEqualTo(0);
        await Assert.That(Inherited).IsNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
public class SingleGenericSetsRequiredFieldTests<T>
{
    public required int Number = 3;

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public SingleGenericSetsRequiredFieldTests(T value)
    {
        Number += (int)(object)value!;
    }

    [Test]
    public async Task Run() => await Assert.That(Number).IsEqualTo(4);
}
