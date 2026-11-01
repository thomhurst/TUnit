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
