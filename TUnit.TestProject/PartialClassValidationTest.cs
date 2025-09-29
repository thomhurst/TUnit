using TUnit.Core;

namespace TUnit.TestProject.PartialClassValidation;

/// <summary>
/// Test to validate that partial classes with ClassDataSource work correctly
/// without generating CS8785 warnings (duplicate hintName in source generators)
/// </summary>
public partial class PartialClassValidationTest
{
    [Test]
    public async Task TestPartialClassCompilation()
    {
        // This test validates that the source generators can handle partial classes
        // without generating duplicate hintName warnings
        await Assert.That(DataSource).IsNotNull();
        await Assert.That(DataSource.TestValue).IsEqualTo("ValidatedTestData");
    }
}

public partial class PartialClassValidationTest  
{
    [ClassDataSource<ValidatedDataSource>(Shared = SharedType.PerTestSession)]
    public required ValidatedDataSource DataSource { get; init; }
}

public class ValidatedDataSource
{
    public string TestValue { get; set; } = "ValidatedTestData";
}