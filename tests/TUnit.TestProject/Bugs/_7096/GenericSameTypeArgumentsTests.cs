using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7096;

/// <summary>
/// Several [Arguments] rows that infer the same generic type argument must each produce a test.
/// Source-gen mode used to keep only the first row per inferred type (#7096).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericMethodSameTypeArgumentsTests
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task SameType<T>(T value)
    {
        await Assert.That(value).IsTypeOf<int>();
    }

    [Test]
    [Arguments(1)]
    [Arguments("a")]
    [Arguments(2)]
    [Arguments("b")]
    public async Task MixedTypes<T>(T value)
    {
        await Assert.That(value is int or string).IsTrue();
    }
}

/// <summary>
/// Per-row settings (Skip, DisplayName, Categories) on rows that share an inferred type must
/// apply only to their own row.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericMethodSameTypeRowSettingsTests
{
    [Test]
    [Arguments(1, Skip = "Only row one is skipped")]
    [Arguments(2)]
    [Arguments(3, DisplayName = "Row three", Categories = new[] { "Issue7096RowCategory" })]
    public async Task RowSettings<T>(T value)
    {
        await Assert.That(value).IsNotEqualTo((T)(object)1);

        var context = TestContext.Current!;
        var isRowThree = Equals(value, 3);

        await Assert.That(context.Metadata.TestDetails.Categories.Contains("Issue7096RowCategory")).IsEqualTo(isRowThree);
        await Assert.That(context.Metadata.DisplayName == "Row three").IsEqualTo(isRowThree);
    }
}
