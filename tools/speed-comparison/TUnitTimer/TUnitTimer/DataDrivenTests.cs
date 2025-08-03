namespace TUnitTimer;

public class DataDrivenTests
{
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(10, 20, 30)]
    [Arguments(-5, 5, 0)]
    [Arguments(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
    {
        var result = a + b;
        Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("hello", "HELLO")]
    [Arguments("world", "WORLD")]
    [Arguments("TUnit", "TUNIT")]
    [Arguments("Testing", "TESTING")]
    [Arguments("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
    {
        var result = input.ToUpper();
        Assert.That(result).IsEqualTo(expected);
        Assert.That(result.Length).IsEqualTo(input.Length);
    }

    [Test]
    [MethodDataSource(nameof(ComplexTestData))]
    public void MethodDataSourceTest(TestData data)
    {
        var result = ProcessTestData(data);
        
        Assert.That(result.Id).IsEqualTo(data.Id);
        Assert.That(result.ProcessedValue).IsEqualTo(data.Value * 2);
        Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [ClassDataSource<TestDataProvider>]
    public void ClassDataSourceTest(int value, string text, bool flag)
    {
        Assert.That(value).IsGreaterThan(0);
        Assert.That(text).IsNotNull();
        Assert.That(text.Length).IsGreaterThan(0);
        Assert.That(flag).IsTrue();
    }

    [Test]
    [Arguments(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [Arguments(new int[] { 10, 20, 30 }, 60)]
    [Arguments(new int[] { -5, 0, 5 }, 0)]
    [Arguments(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
    {
        var sum = numbers.Sum();
        var average = numbers.Average();
        
        Assert.That(sum).IsEqualTo(expectedSum);
        Assert.That(average).IsEqualTo((double)expectedSum / numbers.Length);
        Assert.That(numbers).IsNotEmpty();
    }

    public static IEnumerable<TestData> ComplexTestData()
    {
        yield return new TestData { Id = 1, Value = 10, Name = "Test1" };
        yield return new TestData { Id = 2, Value = 20, Name = "Test2" };
        yield return new TestData { Id = 3, Value = 30, Name = "Test3" };
        yield return new TestData { Id = 4, Value = 40, Name = "Test4" };
        yield return new TestData { Id = 5, Value = 50, Name = "Test5" };
    }

    private ProcessedData ProcessTestData(TestData data)
    {
        return new ProcessedData
        {
            Id = data.Id,
            ProcessedValue = data.Value * 2,
            IsValid = true
        };
    }

    public class TestData
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public string Name { get; set; } = "";
    }

    public class ProcessedData
    {
        public int Id { get; set; }
        public int ProcessedValue { get; set; }
        public bool IsValid { get; set; }
    }

    public class TestDataProvider : IDataSourceGenerator<(int, string, bool)>
    {
        public IEnumerable<(int, string, bool)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return (1, "First", true);
            yield return (2, "Second", true);
            yield return (3, "Third", true);
            yield return (4, "Fourth", true);
            yield return (5, "Fifth", true);
        }
    }
}