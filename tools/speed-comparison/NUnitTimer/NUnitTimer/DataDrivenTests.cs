namespace NUnitTimer;

[TestFixture]
public class DataDrivenTests
{
    [TestCase(1, 2, 3)]
    [TestCase(10, 20, 30)]
    [TestCase(-5, 5, 0)]
    [TestCase(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
    {
        var result = a + b;
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("hello", "HELLO")]
    [TestCase("world", "WORLD")]
    [TestCase("NUnit", "NUNIT")]
    [TestCase("Testing", "TESTING")]
    [TestCase("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
    {
        var result = input.ToUpper();
        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result.Length, Is.EqualTo(input.Length));
    }

    [Test]
    [TestCaseSource(nameof(ComplexTestData))]
    public void TestCaseSourceTest(TestData data)
    {
        var result = ProcessTestData(data);

        Assert.That(result.Id, Is.EqualTo(data.Id));
        Assert.That(result.ProcessedValue, Is.EqualTo(data.Value * 2));
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    [TestCaseSource(typeof(TestDataProvider), nameof(TestDataProvider.GetTestCases))]
    public void ClassDataSourceTest(int value, string text, bool flag)
    {
        Assert.That(value, Is.GreaterThan(0));
        Assert.That(text, Is.Not.Null);
        Assert.That(text.Length, Is.GreaterThan(0));
        Assert.That(flag, Is.True);
    }

    [TestCase(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [TestCase(new int[] { 10, 20, 30 }, 60)]
    [TestCase(new int[] { -5, 0, 5 }, 0)]
    [TestCase(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
    {
        var sum = numbers.Sum();
        var average = numbers.Average();

        Assert.That(sum, Is.EqualTo(expectedSum));
        Assert.That(average, Is.EqualTo((double)expectedSum / numbers.Length));
        Assert.That(numbers, Is.Not.Empty);
    }

    [Test]
    [TestCaseSource(nameof(GetValues))]
    public void TestCaseSource(int value)
    {
        var squared = value * value;
        Assert.That(squared, Is.GreaterThan(0));
        Assert.That(squared, Is.EqualTo(value * value));
    }

    public static IEnumerable<TestData> ComplexTestData()
    {
        yield return new TestData { Id = 1, Value = 10, Name = "Test1" };
        yield return new TestData { Id = 2, Value = 20, Name = "Test2" };
        yield return new TestData { Id = 3, Value = 30, Name = "Test3" };
        yield return new TestData { Id = 4, Value = 40, Name = "Test4" };
        yield return new TestData { Id = 5, Value = 50, Name = "Test5" };
    }

    public static IEnumerable<int> GetValues()
    {
        yield return 1;
        yield return 2;
        yield return 3;
        yield return 4;
        yield return 5;
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

    public static class TestDataProvider
    {
        public static IEnumerable<TestCaseData> GetTestCases()
        {
            yield return new TestCaseData(1, "First", true);
            yield return new TestCaseData(2, "Second", true);
            yield return new TestCaseData(3, "Third", true);
            yield return new TestCaseData(4, "Fourth", true);
            yield return new TestCaseData(5, "Fifth", true);
        }
    }
}
