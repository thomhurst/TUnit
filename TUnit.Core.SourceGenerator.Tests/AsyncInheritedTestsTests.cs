using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AsyncInheritedTestsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AsyncInheritedTestRepro.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsGreaterThan(0);
            
            // Check that the generated code has the proper type casting
            var generatedCode = string.Join("\n", generatedFiles);
            
            // Should have typedInstance casting for InvokeTypedTest
            await Assert.That(generatedCode).Contains("var typedInstance = (global::TUnit.TestProject.DerivedClassWithAsyncInheritance)instance;");
            
            // Should use typedInstance.AsyncTestMethod() instead of instance.AsyncTestMethod()
            await Assert.That(generatedCode).Contains("typedInstance.AsyncTestMethod()");
            
            // Should not have the incorrect instance.AsyncTestMethod() call
            await Assert.That(generatedCode).DoesNotContain("instance.AsyncTestMethod()");
        });
}