using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InstanceMethodTypeInferenceTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7222", "GenericInstanceSourceTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = [Path.Combine(Git.TestsDirectory.FullName,
                "TUnit.TestProject", "Bugs", "_7222", "InstanceSourceConstraintViolationTests.cs")]
        },
        async files =>
        {
            var source = string.Join("\n", files);
            await Assert.That(source).DoesNotContain("Could not infer the type arguments");
            await Assert.That(source).Contains("Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'");
            await Assert.That(source).Contains("GenericInstanceSourceTests<int>");
            await Assert.That(source).Contains("NonGenericInstanceSourceTests");
            await Assert.That(source).DoesNotContain("WholeRow<int>");
            await Assert.That(source).Contains("WholeRow<global::System.Tuple<int, string>>");
            await Assert.That(source).Contains("BothSources<string>");
            await Assert.That(source).Contains("InstanceElement<int>");
            await Assert.That(source).Contains("StaticElement<int>");
            await Assert.That(source).DoesNotContain("InstanceElement<global::System.Tuple<int>>");
            await Assert.That(source).Contains("Valid<int>");
            await Assert.That(source).Contains("Valid<string>");
        });
}
