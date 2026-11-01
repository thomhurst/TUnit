using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7208;

// A class-level [MethodDataSource] that names a member of the generic test class itself (#7208).

[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Strings))]
public class OwnClassMethodDataSource<T>(T value)
{
    public static IEnumerable<string> Strings() => ["s"];

    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(arg).IsTypeOf<string>();
        await Assert.That(value).IsTypeOf<string>();
        await Assert.That(value!.ToString()).IsEqualTo("s");
        await Assert.That(arg!.ToString()).IsEqualTo("a");
    }
}

// The member may be a property, and its rows may be declared as an array.
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Numbers))]
public class OwnClassPropertyDataSource<T>(T value)
{
    public static int[] Numbers => [7];

    [Test]
    [Arguments(2)]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(value!.ToString()).IsEqualTo("7");
        await Assert.That(arg!.ToString()).IsEqualTo("2");
    }
}

// [GenerateGenericTest] supplies the types of a member whose declared rows name the class's own type parameter.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[MethodDataSource(nameof(Values))]
public class OwnClassTypeParameterMethodDataSourceWithGenerateGenericTest<T>(T value)
{
    public static IEnumerable<T> Values() => [(T)(object)5];

    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(value!.ToString()).IsEqualTo("5");
    }
}

// Nothing supplies the types of a member whose declared rows name the class's own type parameter, so the
// class's tests fail, while the rest of the assembly still runs.
[EngineTest(ExpectedResult.Failure)]
[MethodDataSource(nameof(Values))]
public class OwnClassTypeParameterMethodDataSource<T>(T value)
{
    public static IEnumerable<T> Values() => [];

    [Test]
    public void Test()
    {
        _ = value;
    }
}

// Discovery reads the overload invocation reads: the parameterless non-generic one, not the overloads declared
// before it.
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Rows))]
public class OwnClassOverloadedMethodDataSource<T>(T value)
{
    public static IEnumerable<U> Rows<U>() => [];

    public static IEnumerable<T> Rows(int count) => [];

    public static IEnumerable<string> Rows() => ["s"];

    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<string>();
        await Assert.That(value!.ToString()).IsEqualTo("s");
        await Assert.That(arg!.ToString()).IsEqualTo("a");
    }
}

// A member that returns a single value, not a sequence, is one row.
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Row))]
public class OwnClassSingleRowMethodDataSource<T>(T value)
{
    public static string Row() => "s";

    [Test]
    [Arguments("a")]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<string>();
        await Assert.That(value!.ToString()).IsEqualTo("s");
    }
}

// An IAsyncEnumerable member's rows infer the class's types too.
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Rows))]
public class OwnClassAsyncEnumerableMethodDataSource<T>(T value)
{
    public static async IAsyncEnumerable<int> Rows()
    {
        await Task.Yield();
        yield return 7;
    }

    [Test]
    [Arguments(2)]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(value!.ToString()).IsEqualTo("7");
    }
}

// A [GenerateGenericTest] instantiation that class [Arguments] rows also infer still reads the own-class member
// whose rows name the class's type parameter.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[Arguments(1)]
[MethodDataSource(nameof(Values))]
public class OwnClassTypeParameterMethodDataSourceWithArgumentsAndGenerateGenericTest<T>(T value)
{
    public static IEnumerable<T> Values() => [(T)(object)5];

    [Test]
    [Arguments(2)]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(arg!.ToString()).IsEqualTo("2");
    }
}

// The rows infer the types from the constructor the instance is built with (the first one), so a later
// constructor that would infer other types does not reject them.
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Rows))]
public class OwnClassMethodDataSourceWithSeveralConstructors<T>
{
    private readonly T _value;

    public OwnClassMethodDataSourceWithSeveralConstructors(T value) => _value = value;

    public OwnClassMethodDataSourceWithSeveralConstructors(T[] values) => _value = values[0];

    public static IEnumerable<int[]> Rows() => [[7]];

    [Test]
    public async Task Test()
    {
        await Assert.That(_value).IsTypeOf<int[]>();
    }
}

// Repeating a [GenerateGenericTest] that class [Arguments] rows also infer reads the own-class member once.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(int))]
[Arguments(1)]
[MethodDataSource(nameof(Values))]
public class OwnClassTypeParameterMethodDataSourceWithArgumentsAndRepeatedGenerateGenericTest<T>(T value)
{
    public static IEnumerable<T> Values() => [(T)(object)5];

    [Test]
    [Arguments(2)]
    public async Task Test(T arg)
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(arg!.ToString()).IsEqualTo("2");
    }
}

// A parameterless test method on such an instantiation reads the own-class member too: the class rows alone
// make the instantiation, with no method rows.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[Arguments(1)]
[MethodDataSource(nameof(Values))]
public class OwnClassTypeParameterMethodDataSourceWithArgumentsAndGenerateGenericTestParameterless<T>(T value)
{
    public static IEnumerable<T> Values() => [(T)(object)5];

    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int>();
    }
}

// An own-class member whose rows are object[] infers no types either, so the [GenerateGenericTest] instantiation
// that class [Arguments] rows also infer reads it at run time too.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[Arguments(1)]
[MethodDataSource(nameof(Rows))]
public class OwnClassObjectArrayMethodDataSourceWithArgumentsAndGenerateGenericTest<T>(T value)
{
    public static IEnumerable<object[]> Rows() => [[5]];

    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int>();
    }
}

// A tuple row whose generic constructor column is declared as object infers no types either, so it's read on the
// [GenerateGenericTest] instantiation that the class [Arguments] rows also infer (Codex review of #7336)
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[Arguments(1, "a")]
[MethodDataSource(nameof(Rows))]
public class OwnClassObjectTupleMethodDataSourceWithArgumentsAndGenerateGenericTest<T>(T value, string label)
{
    public static IEnumerable<(object, string)> Rows() => [(5, "b")];

    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(label).IsNotNull();
    }
}

// TestDataRow<object> rows name no type, so they're read on the [GenerateGenericTest] instantiation, not on a
// TestDataRow<object> one (Codex review of #7336)
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[Arguments(1)]
[MethodDataSource(nameof(Rows))]
public class OwnClassTestDataRowObjectMethodDataSourceWithArgumentsAndGenerateGenericTest<T>(T value)
{
    public static IEnumerable<TestDataRow<object>> Rows() => [new(5)];

    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int>();
    }
}

// TestDataRow<int> rows infer C<int> from their unwrapped values, not a C<TestDataRow<int>> instantiation that
// can't be built from the unwrapped row (Codex review of #7336)
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[MethodDataSource(nameof(Rows))]
public class OwnClassTestDataRowIntMethodDataSourceWithGenerateGenericTest<T>(T value)
{
    public static IEnumerable<TestDataRow<int>> Rows() => [new(5)];

    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int>();
    }
}

// The rows of an own-class member take the types its declared rows infer through the constructor: int[] rows bound
// to a T[] parameter close C<int>, not C<int[]> (Codex review of #7336)
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[MethodDataSource(nameof(Rows))]
public class OwnClassArrayParameterMethodDataSourceWithGenerateGenericTest<T>(T[] values)
{
    public static IEnumerable<int[]> Rows() => [[5]];

    [Test]
    public async Task Test()
    {
        await Assert.That(values).IsTypeOf<int[]>();
        await Assert.That(values[0]!.ToString()).IsEqualTo("5");
    }
}

// A null row of an own-class member still takes the type its declared rows infer (Greptile review of #7336)
[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Rows))]
public class OwnClassNullTestDataRowMethodDataSource<T>(T value)
{
    public static IEnumerable<TestDataRow<string?>> Rows() => [new(null)];

    [Test]
    public async Task Test()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsNull();
    }
}

// An own-class member whose declared rows infer the types of the [GenerateGenericTest] instantiation that class
// [Arguments] rows also infer is read on it too (Codex review of #7336)
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[Arguments(1)]
[MethodDataSource(nameof(Rows))]
public class OwnClassConcreteMethodDataSourceWithArgumentsAndGenerateGenericTest<T>(T value)
{
    public static IEnumerable<int> Rows() => [5];

    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int>();
    }
}

// Without [GenerateGenericTest], the instantiation that class [Arguments] rows infer still reads an own-class member
// whose declared rows infer the same types (Codex review of #7336)
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[MethodDataSource(nameof(Rows))]
public class OwnClassConcreteMethodDataSourceWithArguments<T>(T value)
{
    public static IEnumerable<int> Rows() => [5];

    [Test]
    public async Task Test()
    {
        await Assert.That(value).IsTypeOf<int>();
    }
}

// The same with a test method that has rows of its own: each class row runs with each method row
[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[MethodDataSource(nameof(Rows))]
public class OwnClassConcreteMethodDataSourceWithArgumentsAndMethodRows<T>(T value)
{
    public static IEnumerable<int> Rows() => [5];

    [Test]
    [Arguments("a")]
    [Arguments("b")]
    public async Task Test(string label)
    {
        await Assert.That(value).IsTypeOf<int>();
        await Assert.That(label).IsNotEmpty();
    }
}

// A tuple row whose other column names T still infers T from its concrete column, so both modes read the member on
// the int instantiation that the [Arguments] row infers (Codex review of #7336)
[EngineTest(ExpectedResult.Pass)]
[Arguments(1, 2)]
[MethodDataSource(nameof(Rows))]
public class OwnClassPartiallyGenericTupleMethodDataSourceWithArguments<T>(T first, T second)
{
    public static IEnumerable<(int, T)> Rows() => [(5, (T)(object)6)];

    [Test]
    public async Task Test()
    {
        await Assert.That(first).IsTypeOf<int>();
        await Assert.That(second).IsTypeOf<int>();
    }
}
