namespace TUnit.TestProject.Bugs._7333;

public static class Rows
{
    public static IEnumerable<int> Integers() => [3];
    public static IEnumerable<int> MoreIntegers() => [4];
    public static Task<IEnumerable<Func<string>>> Strings() => Task.FromResult<IEnumerable<Func<string>>>([() => "three"]);
}

[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class ExternalRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That(value).IsEqualTo((T)(object)3);
}

[MethodDataSource(nameof(Integers))]
public class OwnRows<T>(T value)
{
    public static IEnumerable<int> Integers() => [3];
    [Test]
    public async Task Test() => await Assert.That(value).IsEqualTo((T)(object)3);
}

[MethodDataSource(typeof(Rows), nameof(Rows.Strings))]
public class AsyncFactoryRows<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That(value).IsEqualTo((T)(object)"three");
}

[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
[MethodDataSource(typeof(Rows), nameof(Rows.MoreIntegers))]
public class MultipleSources<T>(T value)
{
    [Test]
    public async Task Test() => await Assert.That((object?)value).IsNotNull();
}

[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class GenericMethod<T>(T value)
{
    [Test]
    [Arguments("method")]
    [Arguments(5)]
    public async Task Test<U>(U methodValue)
    {
        await Assert.That(value).IsEqualTo((T)(object)3);
        await Assert.That((object?)methodValue).IsNotNull();
    }
}

[MethodDataSource(typeof(Rows), nameof(Rows.Integers))]
public class MethodRows<T>(T value)
{
    [Test]
    [Arguments("first")]
    [Arguments("second")]
    public async Task Test(string methodValue)
    {
        await Assert.That(value).IsEqualTo((T)(object)3);
        await Assert.That((object?)methodValue).IsNotNull();
    }
}

// Discovery must report a class whose own member cannot supply its type arguments.
[MethodDataSource(nameof(Values))]
public class UninferableOwnRows<T>(T value)
{
    public static IEnumerable<T> Values() => [];
    [Test]
    public void Test() => _ = value;
}
