using TUnit.Core;

namespace TUnit.TestProject.PartialClass;

public partial class PartialTests
{
    [ClassDataSource<TestDataClass>(Shared = SharedType.PerTestSession)]
    public required TestDataClass DataClass { get; init; }
}

public class TestDataClass
{
    public string Value { get; set; } = "test";
}