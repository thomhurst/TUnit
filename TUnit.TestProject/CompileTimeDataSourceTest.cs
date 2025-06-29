using TUnit.Core;

namespace TUnit.TestProject;

public class CompileTimeDataSourceTest
{
    // Test with multiple Arguments attributes on method
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task MultipleMethodArguments(int value)
    {
        await Assert.That(value).IsGreaterThan(0);
    }

    // Test with method data source
    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public async Task MethodDataSourceTest(int value)
    {
        await Assert.That(value).IsGreaterThan(0);
    }

    public static IEnumerable<int> GetTestData()
    {
        yield return 10;
        yield return 20;
        yield return 30;
    }

    // Test with external type method data source
    [Test]
    [MethodDataSource(typeof(ExternalTestData), nameof(ExternalTestData.GetData))]
    public async Task ExternalMethodDataSourceTest(string value)
    {
        await Assert.That(value).IsNotNull();
    }
}

// Test with multiple Arguments attributes on class
[Arguments(100)]
[Arguments(200)]
public class MultipleClassArgumentsTest
{
    private readonly int _baseValue;

    public MultipleClassArgumentsTest(int baseValue)
    {
        _baseValue = baseValue;
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task ClassAndMethodArguments(int multiplier)
    {
        var result = _baseValue * multiplier;
        await Assert.That(result).IsGreaterThan(0);
    }
}

public static class ExternalTestData
{
    public static IEnumerable<string> GetData()
    {
        yield return "test1";
        yield return "test2";
        yield return "test3";
    }
}
