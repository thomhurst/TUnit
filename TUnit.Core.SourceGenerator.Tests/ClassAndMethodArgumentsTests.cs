using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassAndMethodArgumentsTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassAndMethodArgumentsTests.cs"),
        async generatedFiles =>
        {
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.Simple()");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.Simple()");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"3\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.WithMethodLevel(methodArg)");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"3\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.WithMethodLevel(methodArg)");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"4\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.WithMethodLevel(methodArg)");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"4\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.WithMethodLevel(methodArg)");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"3\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.IgnoreParameters(methodArg)");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"3\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.IgnoreParameters(methodArg)");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"1\";");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"4\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.IgnoreParameters(methodArg)");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg],");
            
            await AssertFileContains(generatedFiles[0], "global::System.String classArg = \"2\";");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"4\";");
            await AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            await AssertFileContains(generatedFiles[0], "classInstance.IgnoreParameters(methodArg)");
            await AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg],");
        });
}