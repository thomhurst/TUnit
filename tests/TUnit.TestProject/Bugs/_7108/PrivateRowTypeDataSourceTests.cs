namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// The rows of these data sources have a private type, so source-generated code cannot name it as a type argument.
/// Reflection mode runs the tests. Source-generated mode cannot close them, so it reports each one as a failed test
/// that explains why, instead of generating code that does not compile. Not an [EngineTest]: the outcome differs
/// between the modes, and Issue7108Tests checks each mode.
/// </summary>
public class PrivateRowTypeDataSourceTests
{
    private sealed class Row(int value)
    {
        public int Value { get; } = value;
    }

    private static IEnumerable<Row> PropertyRows => [new Row(61)];

    private static readonly Row[] FieldRows = [new Row(62)];

    [Test]
    [MethodDataSource(nameof(PropertyRows))]
    public async Task Private_Row_Type_From_Property<T>(T value)
    {
        await Assert.That(value is Row).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(FieldRows))]
    public async Task Private_Row_Type_From_Field<T>(T value)
    {
        await Assert.That(value is Row).IsTrue();
    }
}
