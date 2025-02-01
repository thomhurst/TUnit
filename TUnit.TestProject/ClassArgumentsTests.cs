namespace TUnit.TestProject;

[MatrixDataSource]
[Arguments("str", 1)]
[Arguments("str2", 2)]
[MethodDataSource(nameof(MyMethod))]
[MethodDataSource(nameof(MyMethod2))]
[MethodDataSource(nameof(MyEnumerableMethod))]
public class ClassArgumentsTests(
    [Matrix("matrix1", "matrix2", "matrix3")] string value,
    [Matrix(1, 2)] int number)
{
    [Test]
    public void Without_Timeout()
    {
        Console.Write($@"{value} {number}");
    }

    [Test, Timeout(30_000)]
    public void With_Timeout(CancellationToken cancellationToken)
    {
        Console.Write($@"{value} {number}");
    }

    public static (string, int) MyMethod()
    {
        return ("methodSource1", 1);
    }

    public static (string, int) MyMethod2()
    {
        return ("methodSource2", 2);
    }

    public static IEnumerable<(string, int)> MyEnumerableMethod()
    {
        yield return ("enumerableMethodSource1", 1);
        yield return ("enumerableMethodSource2", 2);
    }
}