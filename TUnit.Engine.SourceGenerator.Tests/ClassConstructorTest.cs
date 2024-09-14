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
                    "Dummy",
                    "SomeAsyncDisposableClass.cs"),
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "DependencyInjectionClassConstructor.cs")
            ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(1));
            Assert.That(generatedFiles[0], Does.Contain("var resettableClassFactory = new ResettableLazy<global::TUnit.TestProject.ClassConstructorTest>(() => new global::TUnit.TestProject.DependencyInjectionClassConstructor().Create<global::TUnit.TestProject.ClassConstructorTest>());"));
        });
}