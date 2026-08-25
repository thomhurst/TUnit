namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Regression: https://github.com/thomhurst/TUnit/issues/6670
/// A derived interface can hide a generic base method with a different return type. The base
/// slot then needs an explicit implementation, which cannot repeat inherited constraints such
/// as <c>notnull</c> (CS0460).
/// </summary>
public class Issue6670Tests : SnapshotTestBase
{
    private const string Source = """
        using System.Collections.Generic;
        using TUnit.Mocks;

        public interface ITest : ITestParent
        {
            new IList<T> Get<T>() where T : notnull;
        }

        public interface ITestParent
        {
            IEnumerable<T> Get<T>() where T : notnull;
        }

        public class TestUsage
        {
            void M()
            {
                var mock = ITest.Mock();
            }
        }
        """;

    [Test]
    public async Task Hidden_Generic_Interface_Method_Does_Not_Repeat_Constraints_On_Explicit_Implementation()
    {
        var errors = GetGeneratedCompilationErrors(Source);
        var genericImplementationErrors = errors
            .Where(error => error.Id is "CS0411" or "CS0460")
            .ToList();

        await Assert.That(genericImplementationErrors).IsEmpty();
    }

    [Test]
    public Task Hidden_Generic_Interface_Method_Generation_Snapshot()
    {
        return VerifyGeneratorOutput(Source);
    }
}
