using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task No_Shared_Argument() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "Dummy",
                    "SomeAsyncDisposableClass.cs")
            ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));

            Assert.That(generatedFiles[0],
                Does.Contain(
                    "global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass methodArg0 = new global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass();"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Class(methodArg0)"));

            Assert.That(generatedFiles[1],
                Does.Contain(
                    "global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass methodArg0 = new global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass();"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class_Generic(methodArg0)"));
        });

    [Test]
    public Task Shared_Argument_Is_None() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTestsSharedNone.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "Dummy",
                    "SomeAsyncDisposableClass.cs")
            ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));

            Assert.That(generatedFiles[0],
                Does.Contain(
                    "global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass methodArg0 = new global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass();"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Class(methodArg0)"));

            Assert.That(generatedFiles[1],
                Does.Contain(
                    "global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass methodArg0 = new global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass();"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class_Generic(methodArg0)"));
        });

    [Test]
    public Task Shared_Argument_Is_ForClass() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTestsSharedForClass.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "Dummy",
                    "SomeAsyncDisposableClass.cs")
            ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));

            Assert.That(generatedFiles[0],
                Does.Contain(
                    "global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass methodArg0 = global::TUnit.Engine.Data.TestDataContainer.GetInstanceForType<global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass>(typeof(global::TUnit.TestProject.ClassDataSourceDrivenTestsSharedForClass), () => new global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass());"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Class(methodArg0)"));

            Assert.That(generatedFiles[1],
                Does.Contain(
                    "global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass methodArg0 = global::TUnit.Engine.Data.TestDataContainer.GetInstanceForType<global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass>(typeof(global::TUnit.TestProject.ClassDataSourceDrivenTestsSharedForClass), () => new global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass());"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class_Generic(methodArg0)"));
        });

    [Test]
    public Task Shared_Argument_Is_Keyed() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests_Shared_Keyed.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "Dummy",
                    "SomeAsyncDisposableClass.cs")
            ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));

            Assert.That(generatedFiles[0],
                Does.Contain(
                    "global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass methodArg0 = global::TUnit.Engine.Data.TestDataContainer.GetInstanceForKey<global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass>(\"🔑\", () => new global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass());"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Class(methodArg0)"));

            Assert.That(generatedFiles[1],
                Does.Contain(
                    "global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass methodArg0 = global::TUnit.Engine.Data.TestDataContainer.GetInstanceForKey<global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass>(\"🔑\", () => new global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass());"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class_Generic(methodArg0)"));
        });
}