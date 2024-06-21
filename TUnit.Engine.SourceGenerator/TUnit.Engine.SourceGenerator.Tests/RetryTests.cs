using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class RetryTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "RetryTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(3));
            
            Assert.That(generatedFiles[0], Does.Contain("RetryCount = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.RetryAttribute>(attributes)?.Times ?? 0,"));
            Assert.That(generatedFiles[1], Does.Contain("RetryCount = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.RetryAttribute>(attributes)?.Times ?? 0,"));
            Assert.That(generatedFiles[2], Does.Contain("RetryCount = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.RetryAttribute>(attributes)?.Times ?? 0,"));
        });
}