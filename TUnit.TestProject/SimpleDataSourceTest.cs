namespace TUnit.TestProject;

public class SimpleDataSourceTest
{
    [Test]
    [MethodDataSource(nameof(GetSimpleData))]
    public async Task SimpleTest(int value)
    {
        Console.WriteLine($"Test running with value: {value}");
        await Assert.That(value).IsGreaterThan(0);
    }

    public static IEnumerable<int> GetSimpleData()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }
}
