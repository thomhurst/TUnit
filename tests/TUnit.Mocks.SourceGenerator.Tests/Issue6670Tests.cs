namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Regression: https://github.com/thomhurst/TUnit/issues/6670
/// A derived interface can hide a generic base method with a different return type. The base
/// slot then needs an explicit implementation, which cannot repeat inherited constraints such
/// as <c>notnull</c> (CS0460).
/// </summary>
public class Issue6670Tests : SnapshotTestBase
{
    [Test]
    public async Task Hidden_Generic_Interface_Method_Does_Not_Repeat_Constraints_On_Explicit_Implementation()
    {
        var source = """
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

        var errors = GetGeneratedCompilationErrors(source);
        var constraintErrors = errors.Where(error => error.Id == "CS0460").ToList();

        await Assert.That(constraintErrors).IsEmpty();
    }
}
