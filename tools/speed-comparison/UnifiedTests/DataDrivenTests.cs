using System.Collections;
using System.Threading.Tasks;

namespace UnifiedTests;

#if MSTEST
[TestClass]
#elif NUNIT
[TestFixture]
#endif
public class DataDrivenTests
{
#if TUNIT
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(10, 20, 30)]
    [Arguments(-5, 5, 0)]
    [Arguments(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
#elif XUNIT || XUNIT3
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(10, 20, 30)]
    [InlineData(-5, 5, 0)]
    [InlineData(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
#elif NUNIT
    [TestCase(1, 2, 3)]
    [TestCase(10, 20, 30)]
    [TestCase(-5, 5, 0)]
    [TestCase(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
#elif MSTEST
    [TestMethod]
    [DataRow(1, 2, 3)]
    [DataRow(10, 20, 30)]
    [DataRow(-5, 5, 0)]
    [DataRow(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
#endif
    {
        var result = a + b;
    }

#if TUNIT
    [Test]
    [Arguments("hello", "HELLO")]
    [Arguments("world", "WORLD")]
    [Arguments("TUnit", "TUNIT")]
    [Arguments("Testing", "TESTING")]
    [Arguments("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
#elif XUNIT || XUNIT3
    [Theory]
    [InlineData("hello", "HELLO")]
    [InlineData("world", "WORLD")]
    [InlineData("xUnit", "XUNIT")]
    [InlineData("Testing", "TESTING")]
    [InlineData("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
#elif NUNIT
    [TestCase("hello", "HELLO")]
    [TestCase("world", "WORLD")]
    [TestCase("NUnit", "NUNIT")]
    [TestCase("Testing", "TESTING")]
    [TestCase("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
#elif MSTEST
    [TestMethod]
    [DataRow("hello", "HELLO")]
    [DataRow("world", "WORLD")]
    [DataRow("MSTest", "MSTEST")]
    [DataRow("Testing", "TESTING")]
    [DataRow("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
#endif
    {
        Console.WriteLine($"{input} {expected}");
    }

#if TUNIT
    [Test]
    [MethodDataSource(nameof(ComplexTestData))]
    public void DataSourceTest(TestData data)
#elif XUNIT || XUNIT3
    [Theory]
    [MemberData(nameof(ComplexTestData))]
    public void DataSourceTest(TestData data)
#elif NUNIT
    [Test]
    [TestCaseSource(nameof(ComplexTestData))]
    public void DataSourceTest(TestData data)
#elif MSTEST
    [TestMethod]
    [DynamicData(nameof(ComplexTestData), DynamicDataSourceType.Method)]
    public void DataSourceTest(TestData data)
#endif
    {
        ProcessTestData(data);
    }

#if TUNIT
    [Test]
    [Arguments(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [Arguments(new int[] { 10, 20, 30 }, 60)]
    [Arguments(new int[] { -5, 0, 5 }, 0)]
    [Arguments(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
#elif XUNIT || XUNIT3
    [Theory]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [InlineData(new int[] { 10, 20, 30 }, 60)]
    [InlineData(new int[] { -5, 0, 5 }, 0)]
    [InlineData(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
#elif NUNIT
    [TestCase(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [TestCase(new int[] { 10, 20, 30 }, 60)]
    [TestCase(new int[] { -5, 0, 5 }, 0)]
    [TestCase(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
#elif MSTEST
    [TestMethod]
    [DataRow(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [DataRow(new int[] { 10, 20, 30 }, 60)]
    [DataRow(new int[] { -5, 0, 5 }, 0)]
    [DataRow(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
#endif
    {
        _ = numbers.Sum();
        _ = numbers.Average();
    }

#if TUNIT
    public static IEnumerable<Func<TestData>> ComplexTestData()
    {
        yield return () => new TestData { Id = 1, Value = 10, Name = "Test1" };
        yield return () => new TestData { Id = 2, Value = 20, Name = "Test2" };
        yield return () => new TestData { Id = 3, Value = 30, Name = "Test3" };
        yield return () => new TestData { Id = 4, Value = 40, Name = "Test4" };
        yield return () => new TestData { Id = 5, Value = 50, Name = "Test5" };
    }
#elif XUNIT || XUNIT3
    public static IEnumerable<object[]> ComplexTestData()
    {
        yield return new object[] { new TestData { Id = 1, Value = 10, Name = "Test1" } };
        yield return new object[] { new TestData { Id = 2, Value = 20, Name = "Test2" } };
        yield return new object[] { new TestData { Id = 3, Value = 30, Name = "Test3" } };
        yield return new object[] { new TestData { Id = 4, Value = 40, Name = "Test4" } };
        yield return new object[] { new TestData { Id = 5, Value = 50, Name = "Test5" } };
    }
#elif NUNIT
    public static IEnumerable<TestData> ComplexTestData()
    {
        yield return new TestData { Id = 1, Value = 10, Name = "Test1" };
        yield return new TestData { Id = 2, Value = 20, Name = "Test2" };
        yield return new TestData { Id = 3, Value = 30, Name = "Test3" };
        yield return new TestData { Id = 4, Value = 40, Name = "Test4" };
        yield return new TestData { Id = 5, Value = 50, Name = "Test5" };
    }
#elif MSTEST
    public static IEnumerable<object[]> ComplexTestData()
    {
        yield return new object[] { new TestData { Id = 1, Value = 10, Name = "Test1" } };
        yield return new object[] { new TestData { Id = 2, Value = 20, Name = "Test2" } };
        yield return new object[] { new TestData { Id = 3, Value = 30, Name = "Test3" } };
        yield return new object[] { new TestData { Id = 4, Value = 40, Name = "Test4" } };
        yield return new object[] { new TestData { Id = 5, Value = 50, Name = "Test5" } };
    }
#endif

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

#if XUNIT
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
#elif TUNIT
    public class TestDataProvider : DataSourceGeneratorAttribute<int, string, bool>
    {
        protected override IEnumerable<Func<(int, string, bool)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => (1, "First", true);
            yield return () => (2, "Second", true);
            yield return () => (3, "Third", true);
            yield return () => (4, "Fourth", true);
            yield return () => (5, "Fifth", true);
        }
    }
#endif
}
