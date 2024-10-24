using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class MethodDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();");
            await AssertFileContains(generatedFiles[0], "classInstance.DataSource_Method(methodArg)");

            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();");
            await AssertFileContains(generatedFiles[0], "classInstance.DataSource_Method2(methodArg)");

            await AssertFileContains(generatedFiles[0], "global::System.Action methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeAction();");
            await AssertFileContains(generatedFiles[0], "classInstance.DataSource_Method_WithAction(methodArg)");
            
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(5)");
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(5)");

            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(\"Hello World!\", 5, true)");
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(\"Hello World!\", 5, true)");
            
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(global::TUnit.TestProject.MethodDataSourceDrivenTests.MyString, 5, true)");
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(global::TUnit.TestProject.MethodDataSourceDrivenTests.MyString, 5, true)");
        });
}