using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassConstructorTest : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassConstructorTest.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "DummyReferenceTypeClass.cs"),
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "DependencyInjectionClassConstructor.cs")
            ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.DependencyInjectionClassConstructor classArg = new global::TUnit.TestProject.DependencyInjectionClassConstructor();");
            await AssertFileContains(generatedFiles[0], "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassConstructorTest>(() => classArg.Create<global::TUnit.TestProject.ClassConstructorTest>());");
            await AssertFileContains(generatedFiles[0], "var resettableClassFactory = resettableClassFactoryDelegate();");
            await AssertFileContains(generatedFiles[0], "ClassConstructor = classArg,");
        });
}