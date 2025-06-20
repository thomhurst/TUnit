using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1939;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<DataClass>]
[ClassDataSource<DataClass>(Shared = SharedType.None)]
[ClassDataSource<DataClass>(Shared = SharedType.Keyed, Key = "")]
[ClassDataSource<DataClass>(Shared = SharedType.PerClass)] // Works as expected
[ClassDataSource<DataClass>(Shared = SharedType.PerAssembly)] // Works as expected
[ClassDataSource<DataClass>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class Tests(DataClass dataClass) : IAsyncDisposable
{
    [Test]
    public void ClassDataSource()
    {
    }

    public async ValueTask DisposeAsync()
    {
        await dataClass.CalledOnTestClassDisposal();
    }
    
    [After(TestSession)]
#pragma warning disable TUnit0042
    public static async Task AssertAllDataClassesDisposed(TestSessionContext context)
#pragma warning restore TUnit0042
    {
        var tests = context.TestClasses
            .FirstOrDefault(x => x.ClassType == typeof(Tests))
            ?.Tests;

        if (tests is null)
        {
            return;
        }
        
        var dataClasses = tests
            .SelectMany(x => x.TestDetails.TestClassArguments)
            .OfType<DataClass>()
            .ToArray();

        using var _ = Assert.Multiple();
        
        await Assert.That(dataClasses).HasCount().EqualTo(6);
        await Assert.That(dataClasses.Where(x => x.Disposed)).HasCount().EqualTo(6);
        
        foreach (var test in tests)
        {
            var dataClass = test.TestDetails.TestClassArguments.OfType<DataClass>().First();

            if (!dataClass.Disposed)
            {
                var classDataSourceAttribute =
                    test.TestDetails.DataAttributes.OfType<ClassDataSourceAttribute<DataClass>>()
                        .First();
                
                throw new Exception($"Not Disposed: {classDataSourceAttribute.Shared}");
            }
        }
    }
}