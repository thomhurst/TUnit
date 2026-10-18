using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7092;

[EngineTest(ExpectedResult.Pass)]
public class SkipIfEmptyMethodDataSourceTests
{
    public static IEnumerable<string> NoRows() => [];

    // Source-gen mode dropped SkipIfEmpty, so this test vanished instead of being reported as skipped.
    [Test]
    [MethodDataSource(nameof(NoRows), SkipIfEmpty = true)]
    public void Empty_Source_With_SkipIfEmpty_Is_Skipped(string value)
    {
        throw new InvalidOperationException($"Should have been skipped, but ran with '{value}'");
    }
}
