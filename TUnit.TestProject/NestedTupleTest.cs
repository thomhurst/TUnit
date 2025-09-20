namespace TUnit.TestProject;

public class NestedTupleTest
{
    // This is what the issue reports - should work but gives TUnit0001 error
    [Test]
    [MethodDataSource(nameof(Data))]
    public void Test_NestedTuple_SeparateParams(int data1, (int, int) data2)
    {
        Console.WriteLine($"data1: {data1}, data2: {data2}");
    }

    // This is an alternative approach with a single tuple parameter
    [Test]
    [MethodDataSource(nameof(Data))]
    public void Test_NestedTuple_SingleParam((int, (int, int)) data)
    {
        Console.WriteLine($"data: {data}");
    }

    // The data source method returning nested tuple
    public static IEnumerable<Func<(int, (int, int))>> Data() => new[]
    {
        () => (1, (2, 3)),
        () => (4, (5, 6))
    };
}
