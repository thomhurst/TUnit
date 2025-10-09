using TUnit.Core.SourceGenerator.Tests;

namespace TestsBase;

internal sealed class InternalTestMethodTests : TestsBase<InternalTestMethodTestsContent>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "InternalTestWithArgumentsTest.cs"));
}
