using System.Collections;

namespace xUnitTimerV3;

public class DataDrivenTests
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(10, 20, 30)]
    [InlineData(-5, 5, 0)]
    [InlineData(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
    {
        var result = a + b;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello", "HELLO")]
    [InlineData("world", "WORLD")]
    [InlineData("xUnit", "XUNIT")]
    [InlineData("Testing", "TESTING")]
    [InlineData("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
    {
        var result = input.ToUpper();
        Assert.Equal(expected, result);
        Assert.Equal(input.Length, result.Length);
    }

    [Theory]
    [MemberData(nameof(ComplexTestData))]
    public void MemberDataSourceTest(TestData data)
    {
        var result = ProcessTestData(data);

        Assert.Equal(data.Id, result.Id);
        Assert.Equal(data.Value * 2, result.ProcessedValue);
        Assert.True(result.IsValid);
    }

    [Theory]
    [ClassData(typeof(TestDataProvider))]
    public void ClassDataSourceTest(int value, string text, bool flag)
    {
        Assert.True(value > 0);
        Assert.NotNull(text);
        Assert.True(text.Length > 0);
        Assert.True(flag);
    }

    [Theory]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [InlineData(new int[] { 10, 20, 30 }, 60)]
    [InlineData(new int[] { -5, 0, 5 }, 0)]
    [InlineData(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
    {
        var sum = numbers.Sum();
        var average = numbers.Average();

        Assert.Equal(expectedSum, sum);
        Assert.Equal((double)expectedSum / numbers.Length, average);
        Assert.NotEmpty(numbers);
    }

    public static IEnumerable<object[]> ComplexTestData()
    {
        yield return new object[] { new TestData { Id = 1, Value = 10, Name = "Test1" } };
        yield return new object[] { new TestData { Id = 2, Value = 20, Name = "Test2" } };
        yield return new object[] { new TestData { Id = 3, Value = 30, Name = "Test3" } };
        yield return new object[] { new TestData { Id = 4, Value = 40, Name = "Test4" } };
        yield return new object[] { new TestData { Id = 5, Value = 50, Name = "Test5" } };
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

    public class TestDataProvider : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { 1, "First", true };
            yield return new object[] { 2, "Second", true };
            yield return new object[] { 3, "Third", true };
            yield return new object[] { 4, "Fourth", true };
            yield return new object[] { 5, "Fifth", true };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
