using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// Per-row settings (Skip, DisplayName, Categories) on class-level [Arguments] rows apply only to
// the tests that row produces, including when several rows of a generic class infer the same type.

[EngineTest(ExpectedResult.Pass)]
[Arguments(1, Skip = "Only class row one is skipped")]
[Arguments(2)]
[Arguments(3, DisplayName = "Class row three", Categories = new[] { "Issue7106ClassRowCategory" })]
public class RowSettingsOnGenericClassRows<T>(T value)
{
    [Test]
    public async Task Run()
    {
        await ClassRowSettingsAssertions.AssertOwnRowSettings(value);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1, Skip = "Only class row one is skipped")]
[Arguments(2)]
[Arguments(3, DisplayName = "Class row three", Categories = new[] { "Issue7106ClassRowCategory" })]
public class RowSettingsOnNonGenericClassRows(int value)
{
    [Test]
    public async Task Run()
    {
        await ClassRowSettingsAssertions.AssertOwnRowSettings(value);
    }
}

internal static class ClassRowSettingsAssertions
{
    public static async Task AssertOwnRowSettings<T>(T value)
    {
        await Assert.That(Equals(value, 1)).IsFalse();

        var context = TestContext.Current!;
        var isRowThree = Equals(value, 3);

        await Assert.That(context.Metadata.TestDetails.Categories.Contains("Issue7106ClassRowCategory")).IsEqualTo(isRowThree);
        await Assert.That(context.Metadata.DisplayName == "Class row three").IsEqualTo(isRowThree);
    }
}
