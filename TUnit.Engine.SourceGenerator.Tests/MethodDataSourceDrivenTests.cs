using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class MethodDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(8);
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();");
            await AssertFileContains(generatedFiles[0], "classInstance.DataSource_Method(methodArg)");
            
            await AssertFileContains(generatedFiles[1], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();");
            await AssertFileContains(generatedFiles[1], "classInstance.DataSource_Method2(methodArg)");
            
            await AssertFileContains(generatedFiles[2], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(5)");
            await AssertFileContains(generatedFiles[3], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(5)");

            await AssertFileContains(generatedFiles[4], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(\"Hello World!\", 5, true)");
            await AssertFileContains(generatedFiles[5], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(\"Hello World!\", 5, true)");
            
            await AssertFileContains(generatedFiles[6], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(global::TUnit.TestProject.MethodDataSourceDrivenTests.MyString, 5, true)");
            await AssertFileContains(generatedFiles[7], "global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod(global::TUnit.TestProject.MethodDataSourceDrivenTests.MyString, 5, true)");
        });
}