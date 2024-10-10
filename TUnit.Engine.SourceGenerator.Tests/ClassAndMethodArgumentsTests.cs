using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassAndMethodArgumentsTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassAndMethodArgumentsTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(10);
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.Simple()");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [],");
            
            await AssertFileContains(generatedFiles[1], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[1], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[1], "classInstance.Simple()");
            await AssertFileContains(generatedFiles[1], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[1], "TestMethodArguments = [],");
            
            await AssertFileContains(generatedFiles[2], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[2], "global::System.String methodArg = \"3\";");
            await AssertFileContains(generatedFiles[2], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[2], "classInstance.WithMethodLevel(methodArg)");
            await AssertFileContains(generatedFiles[2], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[2], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[3], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[3], "global::System.String methodArg = \"3\";");
            await AssertFileContains(generatedFiles[3], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[3], "classInstance.WithMethodLevel(methodArg)");
            await AssertFileContains(generatedFiles[3], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[3], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[4], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[4], "global::System.String methodArg = \"4\";");
            await AssertFileContains(generatedFiles[4], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[4], "classInstance.WithMethodLevel(methodArg)");
            await AssertFileContains(generatedFiles[4], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[4], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[5], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[5], "global::System.String methodArg = \"4\";");
            await AssertFileContains(generatedFiles[5], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[5], "classInstance.WithMethodLevel(methodArg)");
            await AssertFileContains(generatedFiles[5], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[5], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[6], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[6], "global::System.String methodArg = \"3\";");
            await AssertFileContains(generatedFiles[6], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[6], "classInstance.IgnoreParameters(methodArg)");
            await AssertFileContains(generatedFiles[6], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[6], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[7], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[7], "global::System.String methodArg = \"3\";");
            await AssertFileContains(generatedFiles[7], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[7], "classInstance.IgnoreParameters(methodArg)");
            await AssertFileContains(generatedFiles[7], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[7], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[8], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[8], "global::System.String methodArg = \"4\";");
            await AssertFileContains(generatedFiles[8], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[8], "classInstance.IgnoreParameters(methodArg)");
            await AssertFileContains(generatedFiles[8], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[8], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[9], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[9], "global::System.String methodArg = \"4\";");
            await AssertFileContains(generatedFiles[9], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[9], "classInstance.IgnoreParameters(methodArg)");
            await AssertFileContains(generatedFiles[9], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[9], "TestMethodArguments = [methodArg],");
        });
}