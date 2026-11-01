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
