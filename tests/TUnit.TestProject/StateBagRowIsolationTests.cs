using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class StateBagRowIsolationTests
{
    public sealed class RowStateAttribute : DataSourceGeneratorAttribute<int>
    {
        protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            for (var i = 1; i <= 2; i++)
            {
                var row = i;
                metadata.TestBuilderContext.Current.StateBag["enumerationRow"] = row;
                yield return () =>
                {
                    var context = metadata.TestBuilderContext.Current;
                    if (!context.StateBag.TryAdd("factoryResource", new object()))
                    {
                        throw new InvalidOperationException("Factory inherited another test's resource");
                    }

                    context.StateBag.TryAdd("factoryRow", row);
                    return row;
                };
            }
        }
    }

    [Test, RowState, Repeat(1), EngineTest(ExpectedResult.Pass)]
    public async Task RowsAndRepeatsHaveIndependentState(int row)
    {
        await Assert.That(TestContext.Current!.StateBag["enumerationRow"]).IsEqualTo(row);
        await Assert.That(TestContext.Current.StateBag["factoryRow"]).IsEqualTo(row);
    }
}
