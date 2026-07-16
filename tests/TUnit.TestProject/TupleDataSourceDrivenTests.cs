using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TupleDataSourceDrivenTests
{
    [Test]
    [MethodDataSource(nameof(TupleMethod))]
    public void DataSource_TupleMethod(int value, string value2, bool value3)
    {
        // Dummy method
    }

    public static (int, string, bool) TupleMethod() => (1, "String", true);
}
