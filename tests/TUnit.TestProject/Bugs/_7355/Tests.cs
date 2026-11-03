namespace TUnit.TestProject.Bugs._7355;

public static class Sources
{
    public static IEnumerable<object> Counters() => [new TestDataRow<Func<Counter>>(() => new Counter())];
}

public sealed class Counter;

[MethodDataSource(typeof(Sources), nameof(Sources.Counters))]
public class OnlyUntypedClassSourceInfersT<T>(T value)
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public void Test(int arg) { }
}

[GenerateGenericTest(typeof(Counter))]
[MethodDataSource(typeof(Sources), nameof(Sources.Counters))]
public class ExplicitClassSource<T>(T value)
{
    [Test]
    public void Test() { }
}

[Arguments(1)]
public class ArgumentsClassSource<T>(T value)
{
    [Test]
    public void Test() { }
}

public class OpenTemplate<T>
{
    [Test]
    public void Test() { }
}

[MethodDataSource(typeof(Sources), nameof(Sources.Counters))]
public abstract class InheritedClassSourceBase;

public class InheritedUntypedClassSource<T>(T value) : InheritedClassSourceBase
{
    [Test]
    public void Test() { }
}

[Arguments(1)]
public class FilteredClassSource<T>(T value) where T : struct
{
    [Test]
    [Arguments("a")]
    public void Test(T arg) { }
}
