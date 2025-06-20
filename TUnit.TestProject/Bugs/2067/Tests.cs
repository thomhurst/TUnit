using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2067;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<DataClass>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class Tests(DataClass dataClass)
{
    [Test]
    public void ClassDataSource()
    {
        Console.WriteLine(dataClass);
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

        var dataClass = await Assert.That(dataClasses).HasSingleItem();
        
        await Assert.That(dataClass).IsNotNull();
        await Assert.That(dataClass!.IsRegistered).IsTrue();
        await Assert.That(dataClass.IsInitialized).IsTrue();
        await Assert.That(dataClass.IsStarted).IsTrue();
        await Assert.That(dataClass.IsEnded).IsTrue();
        await Assert.That(dataClass.IsDisposed).IsTrue();
    }
}