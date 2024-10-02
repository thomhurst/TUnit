using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ConcreteClassTests : TestsBase<InheritsTestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AbstractTests",
            "ConcreteClass2.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "AbstractTests",
                    "AbstractBaseClass.cs"),
                
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "AbstractTests",
                    "ConcreteClass1.cs"),
            ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(3));

            Assert.That(generatedFiles[0],
                Does.Contain("TestId = $\"TUnit.TestProject.AbstractTests.ConcreteClass2.AssertClassName:0\","));
            Assert.That(generatedFiles[0],
                Does.Contain("var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.AbstractTests.ConcreteClass2>(() => new global::TUnit.TestProject.AbstractTests.ConcreteClass2());"));
            
            Assert.That(generatedFiles[1],
                Does.Contain("TestId = $\"TUnit.TestProject.AbstractTests.ConcreteClass2.SecondTest:0\","));
            Assert.That(generatedFiles[1],
                Does.Contain("var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.AbstractTests.ConcreteClass2>(() => new global::TUnit.TestProject.AbstractTests.ConcreteClass2());"));
            
            Assert.That(generatedFiles[2],
                Does.Contain("TestId = $\"TUnit.TestProject.AbstractTests.ConcreteClass1.AssertClassName:0\","));
            Assert.That(generatedFiles[2],
                Does.Contain("var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.AbstractTests.ConcreteClass1>(() => new global::TUnit.TestProject.AbstractTests.ConcreteClass1());"));
        });
}