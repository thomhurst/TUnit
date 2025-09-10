namespace TUnit.Core.SourceGenerator.Tests;

internal class ExtensionMethodStaticClassTests : TestsBase<PropertyInjectionSourceGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ExtensionMethodStaticClassTests.cs"),
        async generatedFiles =>
        {
            // The PropertyInjectionSourceGenerator should not generate any code for static extension method classes
            // There should be only the empty module initializer  
            if (generatedFiles.Length > 0)
            {
                // If any files are generated, they should not contain references to static extension classes
                foreach (var file in generatedFiles)
                {
                    await Assert.That(file).DoesNotContain("PhoneNumberFixtures");
                    await Assert.That(file).DoesNotContain("FirstNameFixtures");
                    await Assert.That(file).DoesNotContain("extension");
                    
                    // Should only contain the empty module initializer
                    await Assert.That(file).Contains("PropertyInjectionInitializer");
                    await Assert.That(file).Contains("InitializePropertyInjectionSources()");
                }
            }
        });
}