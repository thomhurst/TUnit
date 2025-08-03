namespace MSTestTimer;

[TestClass]
public class DataDrivenTests
{
    [DataTestMethod]
    [DataRow(1, 2, 3)]
    [DataRow(10, 20, 30)]
    [DataRow(-5, 5, 0)]
    [DataRow(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
    {
        var result = a + b;
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    [DataRow("hello", "HELLO")]
    [DataRow("world", "WORLD")]
    [DataRow("MSTest", "MSTEST")]
    [DataRow("Testing", "TESTING")]
    [DataRow("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
    {
        var result = input.ToUpper();
        Assert.AreEqual(expected, result);
        Assert.AreEqual(input.Length, result.Length);
    }

    [DataTestMethod]
    [DynamicData(nameof(ComplexTestData), DynamicDataSourceType.Method)]
    public void DynamicDataSourceTest(TestData data)
    {
        var result = ProcessTestData(data);
        
        Assert.AreEqual(data.Id, result.Id);
        Assert.AreEqual(data.Value * 2, result.ProcessedValue);
        Assert.IsTrue(result.IsValid);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetTestCases), DynamicDataSourceType.Method)]
    public void ClassDataSourceTest(int value, string text, bool flag)
    {
        Assert.IsTrue(value > 0);
        Assert.IsNotNull(text);
        Assert.IsTrue(text.Length > 0);
        Assert.IsTrue(flag);
    }

    [DataTestMethod]
    [DataRow(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [DataRow(new int[] { 10, 20, 30 }, 60)]
    [DataRow(new int[] { -5, 0, 5 }, 0)]
    [DataRow(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
    {
        var sum = numbers.Sum();
        var average = numbers.Average();
        
        Assert.AreEqual(expectedSum, sum);
        Assert.AreEqual((double)expectedSum / numbers.Length, average);
        Assert.IsTrue(numbers.Length > 0);
    }

    public static IEnumerable<object[]> ComplexTestData()
    {
        yield return new object[] { new TestData { Id = 1, Value = 10, Name = "Test1" } };
        yield return new object[] { new TestData { Id = 2, Value = 20, Name = "Test2" } };
        yield return new object[] { new TestData { Id = 3, Value = 30, Name = "Test3" } };
        yield return new object[] { new TestData { Id = 4, Value = 40, Name = "Test4" } };
        yield return new object[] { new TestData { Id = 5, Value = 50, Name = "Test5" } };
    }

    public static IEnumerable<object[]> GetTestCases()
    {
        yield return new object[] { 1, "First", true };
        yield return new object[] { 2, "Second", true };
        yield return new object[] { 3, "Third", true };
        yield return new object[] { 4, "Fourth", true };
        yield return new object[] { 5, "Fifth", true };
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
}