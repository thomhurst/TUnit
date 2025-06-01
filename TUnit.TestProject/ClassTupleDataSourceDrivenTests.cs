using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(TupleMethod))]
[MethodDataSource(nameof(NamedTupleMethod))]
public class ClassTupleDataSourceDrivenTests
{
    [MethodDataSource(nameof(TupleMethod))]
    public required (int, string, bool) Property1 { get; init; }

    [MethodDataSource(nameof(NamedTupleMethod))]
    public required (int, string, bool) Property2 { get; init; }

    [MethodDataSource(nameof(TupleMethod))]
    public required (int Number, string Word, bool Flag) Property3 { get; init; }

    [MethodDataSource(nameof(NamedTupleMethod))]
    public required (int Number, string Word, bool Flag) Property4 { get; init; }

    public ClassTupleDataSourceDrivenTests(int value, string value2, bool value3)
    {
        // Dummy method
    }

    [Test]
    [MethodDataSource(nameof(TupleMethod))]
    [MethodDataSource(nameof(NamedTupleMethod))]
    public void DataSource_TupleMethod(int value, string value2, bool value3)
    {
        // Dummy method
    }

    public static Func<(int, string, bool)> TupleMethod() => () => (1, "String", true);
    public static Func<(int Number, string Word, bool Flag)> NamedTupleMethod() => () => (1, "String", true);

}