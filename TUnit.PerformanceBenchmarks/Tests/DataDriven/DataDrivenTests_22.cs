namespace TUnit.PerformanceBenchmarks.Tests.DataDriven;

public class DataDrivenTests_22
{
    public static IEnumerable<(int, string)> TestData()
    {
        for (int i = 0; i < 10; i++)
            yield return (i, "Value" + i);
    }

    [Test]
    [MethodDataSource(nameof(TestData))]
    public void DataTest_01((int num, string str) data) { _ = data.num + data.str.Length; }

    [Test]
    [MethodDataSource(nameof(TestData))]
    public void DataTest_02((int num, string str) data) { _ = data.num + data.str.Length; }

    [Test]
    [MethodDataSource(nameof(TestData))]
    public void DataTest_03((int num, string str) data) { _ = data.num + data.str.Length; }
}
