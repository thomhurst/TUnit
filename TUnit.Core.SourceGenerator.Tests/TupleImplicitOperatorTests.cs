namespace TUnit.Core.SourceGenerator.Tests;

public class TupleImplicitOperatorTests : TestsBase
{
    [Test]
    public async Task Test()
    {
        await RunTest(Path.Combine(Git.RootDirectory.FullName,
                "TUnit.Core.SourceGenerator.Tests", "Bugs", "TupleImplicitOperator.cs"),
            async generatedFiles =>
            {
                // No verification needed, just that it compiles successfully
            });
    }
}