namespace TUnit.TestProject;

public class EnumerableTupleDataSourceDrivenTests
{
    [Test]
    [MethodDataSource(nameof(TupleMethod))]
    [MethodDataSource(nameof(NamedTupleMethod))]
    public void DataSource_TupleMethod(int value, string value2, bool value3)
    {
        // Dummy method
    }

    public static IEnumerable<(int, string, bool)> TupleMethod()
    {
        yield return (1, "String", true);
        yield return (2, "String2", false);
        yield return (3, "String3", true);
    }

    public static IEnumerable<(int value1, string value2, bool value3)> NamedTupleMethod()
    {
        yield return (1, "String", true);
        yield return (2, "String2", false);
        yield return (3, "String3", true);
    }
}