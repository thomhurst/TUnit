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

[ClassConstructor<TemporaryConstructorRowStateTests.RowConstructor>]
[StateBagRowIsolationTests.RowState]
public class TemporaryConstructorRowStateTests(int row)
{
    public sealed class RowConstructor : TUnit.Core.Interfaces.IClassConstructor
    {
        public Task<object> Create(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
            ClassConstructorMetadata metadata)
        {
            var state = metadata.TestBuilderContext.StateBag;
            if (!state.TryAdd("constructorVisited", true))
                throw new InvalidOperationException("Constructor inherited another instance's state");
            return Task.FromResult<object>(new TemporaryConstructorRowStateTests((int)state["factoryRow"]!));
        }
    }

    public IEnumerable<int> Rows() => [row];

    [Test, InstanceMethodDataSource(nameof(Rows)), Repeat(1), EngineTest(ExpectedResult.Pass)]
    public async Task TemporaryConstructorSeesFactoryState(int methodRow)
    {
        await Assert.That(methodRow).IsEqualTo(row);
        await Assert.That(TestContext.Current!.StateBag["factoryRow"]).IsEqualTo(row);
    }
}
