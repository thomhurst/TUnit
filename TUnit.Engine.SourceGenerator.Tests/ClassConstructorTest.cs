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
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(1));
            AssertFileContains(generatedFiles[0], "var classConstructor = new global::TUnit.TestProject.DependencyInjectionClassConstructor();");
            AssertFileContains(generatedFiles[0], "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassConstructorTest>(() => classConstructor.Create<global::TUnit.TestProject.ClassConstructorTest>());");
            AssertFileContains(generatedFiles[0], "var resettableClassFactory = resettableClassFactoryDelegate();");
            AssertFileContains(generatedFiles[0], "ClassConstructor = classConstructor,");
        });
}