using TUnit.Assertions.Extensions;
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
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(3);

            await AssertFileContains(generatedFiles[0], "TestId = $\"TUnit.TestProject.AbstractTests.ConcreteClass2.AssertClassName:0\",");
            await AssertFileContains(generatedFiles[0], "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.AbstractTests.ConcreteClass2>(() => new global::TUnit.TestProject.AbstractTests.ConcreteClass2());");
            
            await AssertFileContains(generatedFiles[1], "TestId = $\"TUnit.TestProject.AbstractTests.ConcreteClass2.SecondTest:0\",");
            await AssertFileContains(generatedFiles[1], "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.AbstractTests.ConcreteClass2>(() => new global::TUnit.TestProject.AbstractTests.ConcreteClass2());");
            
            await AssertFileContains(generatedFiles[2], "TestId = $\"TUnit.TestProject.AbstractTests.ConcreteClass1.AssertClassName:0\",");
            await AssertFileContains(generatedFiles[2], "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.AbstractTests.ConcreteClass1>(() => new global::TUnit.TestProject.AbstractTests.ConcreteClass1());");
        });
}