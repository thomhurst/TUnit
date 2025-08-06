using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

public class TupleImplicitOperatorTests : TestsBase<TestMetadataGenerator>
{
    [Test]
    public async Task Test()
    {
        await RunTest(Path.Combine(Git.RootDirectory.FullName,
                "TUnit.Core.SourceGenerator.Tests", "Bugs", "TupleImplicitOperator.cs"),
            new RunTestOptions());
    }
}