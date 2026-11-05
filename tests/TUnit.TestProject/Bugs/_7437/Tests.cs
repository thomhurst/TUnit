[assembly: TUnit.Core.Category("Issue7437Assembly")]
[assembly: TUnit.Core.Property("Issue7437Scope", "Assembly")]

namespace TUnit.TestProject.Bugs._7437;

[Category("Issue7437Base")]
[Property("Issue7437Scope", "Base")]
public abstract class FilteredFailureBase;

[GenerateGenericTest(typeof(string))]
[Category("Issue7437Class")]
[Property("Issue7437Scope", "Class")]
public class InvalidClass<T> : FilteredFailureBase where T : struct
{
    [Test]
    [Category("Issue7437Method")]
    [Property("Issue7437Scope", "Method")]
    public void Run() { }
}

[Category("Issue7437Class")]
[Property("Issue7437Scope", "Class")]
public class InvalidMethod : FilteredFailureBase
{
    [Test]
    [GenerateGenericTest(typeof(string))]
    [Category("Issue7437Method")]
    [Property("Issue7437Scope", "Method")]
    public void GenericRun<T>() where T : struct { }
}

[MethodDataSource(typeof(UntypedSource), nameof(UntypedSource.Rows))]
[Category("Issue7437Class")]
[Property("Issue7437Scope", "Class")]
public class UninferredClass<T>(T value) : FilteredFailureBase
{
    public T Value { get; } = value;

    [Test]
    [Category("Issue7437Method")]
    [Property("Issue7437Scope", "Method")]
    public void Run() { }
}

public static class UntypedSource
{
    public static IEnumerable<object> Rows() => [new object()];
}
