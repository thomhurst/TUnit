using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task No_Shared_Argument() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("global::TUnit.TestProject.ClassDataSourceDrivenTests.SomeAsyncDisposableClass methodArg0 = new global::TUnit.TestProject.ClassDataSourceDrivenTests.SomeAsyncDisposableClass();"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Class(methodArg0)"));
            
            Assert.That(generatedFiles[1], Does.Contain("global::TUnit.TestProject.ClassDataSourceDrivenTests.SomeAsyncDisposableClass methodArg0 = new global::TUnit.TestProject.ClassDataSourceDrivenTests.SomeAsyncDisposableClass();"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class_Generic(methodArg0)"));
        });
    
    [Test]
    public Task Shared_Argument_Is_None() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests_Shared_None.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_None.SomeAsyncDisposableClass methodArg0 = new global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_None.SomeAsyncDisposableClass();"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Class(methodArg0)"));
            
            Assert.That(generatedFiles[1], Does.Contain("global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_None.SomeAsyncDisposableClass methodArg0 = new global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_None.SomeAsyncDisposableClass();"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class_Generic(methodArg0)"));
        });
    
    [Test]
    public Task Shared_Argument_Is_ForClass() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests_Shared_ForClass.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_ForClass.SomeAsyncDisposableClass methodArg0 = global::TUnit.Engine.Data.TestDataContainer.GetInstanceForType<global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_ForClass.SomeAsyncDisposableClass>(typeof(global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_ForClass), () => new global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_ForClass.SomeAsyncDisposableClass());"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Class(methodArg0)"));
            
            Assert.That(generatedFiles[1], Does.Contain("global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_ForClass.SomeAsyncDisposableClass methodArg0 = global::TUnit.Engine.Data.TestDataContainer.GetInstanceForType<global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_ForClass.SomeAsyncDisposableClass>(typeof(global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_ForClass), () => new global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_ForClass.SomeAsyncDisposableClass());"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class_Generic(methodArg0)"));
        });
    
    [Test]
    public Task Shared_Argument_Is_Keyed() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests_Shared_Keyed.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_Keyed.SomeAsyncDisposableClass methodArg0 = global::TUnit.Engine.Data.TestDataContainer.GetInstanceForKey<global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_Keyed.SomeAsyncDisposableClass>(\"🔑\", () => new global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_Keyed.SomeAsyncDisposableClass());"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Class(methodArg0)"));
            
            Assert.That(generatedFiles[1], Does.Contain("global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_Keyed.SomeAsyncDisposableClass methodArg0 = global::TUnit.Engine.Data.TestDataContainer.GetInstanceForKey<global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_Keyed.SomeAsyncDisposableClass>(\"🔑\", () => new global::TUnit.TestProject.ClassDataSourceDrivenTests_Shared_Keyed.SomeAsyncDisposableClass());"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class_Generic(methodArg0)"));
        });
}