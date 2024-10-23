using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AbstractTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task AbstractClass() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AbstractTests",
            "AbstractBaseClass.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsEmpty();
        });
    
    [Test]
    public Task Concrete1() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AbstractTests",
            "ConcreteClass1.cs"),
        new RunTestOptions
        {
            AdditionalFiles = 
            [
                Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "AbstractTests", "AbstractBaseClass.cs")
            ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            // await AssertFileContains(generatedFiles[0], "ReturnType = typeof(void),");
        });
    
    [Test]
    public Task Concrete2() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AbstractTests",
            "ConcreteClass2.cs"),
        new RunTestOptions
        {
            AdditionalFiles = 
            [
                Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "AbstractTests", "ConcreteClass1.cs"),
                Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "AbstractTests", "AbstractBaseClass.cs")
            ]
        },
        async generatedFiles =>
        {
            // Extra generation because we added ConcreteClass1 to the compilation (because it's a base class and therefore a required dependency)
            await Assert.That(generatedFiles.Length).IsEqualTo(3);
            // await AssertFileContains(generatedFiles[0], "ReturnType = typeof(void),");
        });
}