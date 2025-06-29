namespace TUnit.TestProject;

public class StaticTestDefinitionTest
{
    [Test]
    public void SimpleStaticTest()
    {
        // This test should use StaticTestDefinition because:
        // 1. The class is not generic
        // 2. The method is not generic
        // 3. No data sources that require runtime resolution
        Console.WriteLine("Running static test");
    }

    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 6)]
    public void StaticTestWithArguments(int a, int b, int c)
    {
        // This test should also use StaticTestDefinition
        // because ArgumentsAttribute provides compile-time constants
        Console.WriteLine($"Running static test with args: {a}, {b}, {c}");
    }

    [Test]
    [MethodDataSource(nameof(GetData))]
    public void DynamicTestWithMethodDataSource(int value)
    {
        // This test should use DynamicTestMetadata
        // because MethodDataSource requires runtime resolution
        Console.WriteLine($"Running dynamic test with value: {value}");
    }

    public static IEnumerable<int> GetData()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }
}
