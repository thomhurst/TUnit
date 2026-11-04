namespace TUnit.TestProject.Bugs._7426;

public static class Sources
{
    public static IEnumerable<object> Rows() => [new TestDataRow<Func<int>>(() => 42)];
}

[MethodDataSource(typeof(Sources), nameof(Sources.Rows))]
public abstract class DataSourceBase;

// Runtime-only row types cannot infer T in source-generated discovery (#7355).
public class InheritedRuntimeSource<T>(T value) : DataSourceBase
{
    [Test]
    public void Test() => Verify(value);

    internal static void Verify(T actual)
    {
        if (actual is not int number || number != 42)
        {
            throw new InvalidOperationException("Expected the inherited constructor data row.");
        }
    }
}

[GenerateGenericTest(typeof(int))]
public class InheritedExplicitSource<T>(T value) : DataSourceBase
{
    [Test]
    public void Test() => InheritedRuntimeSource<T>.Verify(value);
}

[MethodDataSource(typeof(Sources), nameof(Sources.Rows))]
public class DirectRuntimeSource<T>(T value)
{
    [Test]
    public void Test() => InheritedRuntimeSource<T>.Verify(value);
}

[Arguments(42)]
public abstract class ArgumentsBase;

public class InheritedArguments<T>(T value) : ArgumentsBase
{
    [Test]
    public void Test()
    {
        if (value is not int number || number != 42)
        {
            throw new InvalidOperationException("Expected inherited arguments.");
        }
    }
}
