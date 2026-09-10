namespace TUnit.TestProject.Bugs._3231;

public partial class Tests
{
    [ClassDataSource<DataClass>(Shared = SharedType.PerTestSession)]
    public required DataClass DataClass { get; init; }
}