using System.Collections;
using System.Threading.Tasks;

namespace UnifiedTests;

[TestClass]
public class DataDrivenTests
{
    [DataDrivenTest]
    [TestData(1, 2, 3)]
    [TestData(10, 20, 30)]
    [TestData(-5, 5, 0)]
    [TestData(100, 200, 300)]
    public void ParameterizedAdditionTest(int a, int b, int expected)
    {
        var result = a + b;
        var doubled = result * 2;
    }

    [DataDrivenTest]
    [TestData("hello", "HELLO")]
    [TestData("world", "WORLD")]
    [TestData("TUnit", "TUNIT")]
    [TestData("Testing", "TESTING")]
    [TestData("Framework", "FRAMEWORK")]
    public void ParameterizedStringTest(string input, string expected)
    {
        var upper = input.ToUpper();
        var length = input.Length + expected.Length;
    }

    [DataDrivenTest]
#if MSTEST
    [TestDataSource(nameof(ComplexTestData), DynamicDataSourceType.Method)]
#else
    [TestDataSource(nameof(ComplexTestData))]
#endif
    public void DataSourceTest(TestData data)
    {
        var result = ProcessTestData(data);
        var total = result.ProcessedValue + data.Value;
    }

    [DataDrivenTest]
    [TestData(new int[] { 1, 2, 3, 4, 5 }, 15)]
    [TestData(new int[] { 10, 20, 30 }, 60)]
    [TestData(new int[] { -5, 0, 5 }, 0)]
    [TestData(new int[] { 100 }, 100)]
    public void ArrayParameterTest(int[] numbers, int expectedSum)
    {
        var sum = numbers.Sum();
        var average = numbers.Length > 0 ? numbers.Average() : 0;
        var combined = sum + expectedSum;
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
