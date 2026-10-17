using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7092;

[EngineTest(ExpectedResult.Pass)]
public class EmptyMethodDataSourceTests
{
    private static readonly ConcurrentBag<int> Runs = [];

    public static IEnumerable<object[]> NoRows() => [];

    // An empty source without SkipIfEmpty runs the test once with no arguments, as NoDataSource does (#2862).
    // Reflection mode already did this; source-gen mode produced no test at all.
    [Test]
    [MethodDataSource(nameof(NoRows))]
    public async Task Empty_Source_Runs_Once_With_No_Arguments()
    {
        Runs.Add(1);
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();
        await Assert.That(Runs.Count).IsEqualTo(1);
    }
}
