using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class AbstractTests : TestsBase<InheritsTestsGenerator>
{
    [Test]
    public Task AbstractClass() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AbstractTests",
            "AbstractBaseClass.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles, Is.Empty);
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
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(1));
            // Assert.That(generatedFiles[0], Does.Contain("ReturnType = typeof(void),"));
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
        generatedFiles =>
        {
            // Extra generation because we added ConcreteClass1 to the compilation (because it's a base class and therefore a required dependency)
            Assert.That(generatedFiles.Length, Is.EqualTo(3));
            // Assert.That(generatedFiles[0], Does.Contain("ReturnType = typeof(void),"));
        });
}