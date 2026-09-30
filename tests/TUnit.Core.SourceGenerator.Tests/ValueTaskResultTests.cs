using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ValueTaskResultTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "ValueTaskResultTests.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            var generatedCode = string.Join(Environment.NewLine, generatedFiles);

            await Assert.That(generatedCode)
                .Contains("new global::System.Threading.Tasks.ValueTask(instance.GenericValueTaskResult().AsTask())");
            await Assert.That(generatedCode)
                .Contains("new global::System.Threading.Tasks.ValueTask(instance.GenericMethodValueTaskResult<int>().AsTask())");
        });
}
