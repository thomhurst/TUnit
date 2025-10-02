using TUnit.TestProject.Attributes;
using TUnit.TestProject.Library;

namespace TUnit.TestProject;

// Derived class that inherits the async tests from a different assembly
[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class AsyncInheritedTestsRepro : AsyncBaseTests
{
    [Test]
    public async Task DerivedAsyncTest()
    {
        await Task.Delay(1);
        Console.WriteLine("Derived async test executed");
    }

    [Test]
    public void DerivedSyncTest()
    {
        Console.WriteLine("Derived sync test executed");
    }
}

public class MyGenerator<T>(string tag) : DataSourceGeneratorAttribute<T>
{
    protected override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Activator.CreateInstance<T>()
    }
}
