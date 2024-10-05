using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassAndMethodArgumentsTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassAndMethodArgumentsTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(10));
            
            AssertFileContains(generatedFiles[0], "global::System.String classArg = \"1\";");
            AssertFileContains(generatedFiles[0], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[0], "classInstance.Simple()");
            AssertFileContains(generatedFiles[0], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[0], "TestMethodArguments = [],");
            
            AssertFileContains(generatedFiles[1], "global::System.String classArg = \"2\";");
            AssertFileContains(generatedFiles[1], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[1], "classInstance.Simple()");
            AssertFileContains(generatedFiles[1], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[1], "TestMethodArguments = [],");
            
            AssertFileContains(generatedFiles[2], "global::System.String classArg = \"1\";");
            AssertFileContains(generatedFiles[2], "global::System.String methodArg = \"3\";");
            AssertFileContains(generatedFiles[2], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[2], "classInstance.WithMethodLevel(methodArg)");
            AssertFileContains(generatedFiles[2], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[2], "TestMethodArguments = [methodArg],");
            
            AssertFileContains(generatedFiles[3], "global::System.String classArg = \"2\";");
            AssertFileContains(generatedFiles[3], "global::System.String methodArg = \"3\";");
            AssertFileContains(generatedFiles[3], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[3], "classInstance.WithMethodLevel(methodArg)");
            AssertFileContains(generatedFiles[3], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[3], "TestMethodArguments = [methodArg],");
            
            AssertFileContains(generatedFiles[4], "global::System.String classArg = \"1\";");
            AssertFileContains(generatedFiles[4], "global::System.String methodArg = \"4\";");
            AssertFileContains(generatedFiles[4], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[4], "classInstance.WithMethodLevel(methodArg)");
            AssertFileContains(generatedFiles[4], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[4], "TestMethodArguments = [methodArg],");
            
            AssertFileContains(generatedFiles[5], "global::System.String classArg = \"2\";");
            AssertFileContains(generatedFiles[5], "global::System.String methodArg = \"4\";");
            AssertFileContains(generatedFiles[5], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[5], "classInstance.WithMethodLevel(methodArg)");
            AssertFileContains(generatedFiles[5], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[5], "TestMethodArguments = [methodArg],");
            
            AssertFileContains(generatedFiles[6], "global::System.String classArg = \"1\";");
            AssertFileContains(generatedFiles[6], "global::System.String methodArg = \"3\";");
            AssertFileContains(generatedFiles[6], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[6], "classInstance.IgnoreParameters(methodArg)");
            AssertFileContains(generatedFiles[6], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[6], "TestMethodArguments = [methodArg],");
            
            AssertFileContains(generatedFiles[7], "global::System.String classArg = \"2\";");
            AssertFileContains(generatedFiles[7], "global::System.String methodArg = \"3\";");
            AssertFileContains(generatedFiles[7], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[7], "classInstance.IgnoreParameters(methodArg)");
            AssertFileContains(generatedFiles[7], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[7], "TestMethodArguments = [methodArg],");
            
            AssertFileContains(generatedFiles[8], "global::System.String classArg = \"1\";");
            AssertFileContains(generatedFiles[8], "global::System.String methodArg = \"4\";");
            AssertFileContains(generatedFiles[8], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[8], "classInstance.IgnoreParameters(methodArg)");
            AssertFileContains(generatedFiles[8], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[8], "TestMethodArguments = [methodArg],");
            
            AssertFileContains(generatedFiles[9], "global::System.String classArg = \"2\";");
            AssertFileContains(generatedFiles[9], "global::System.String methodArg = \"4\";");
            AssertFileContains(generatedFiles[9], "new global::TUnit.TestProject.ClassAndMethodArgumentsTests(classArg)");
            AssertFileContains(generatedFiles[9], "classInstance.IgnoreParameters(methodArg)");
            AssertFileContains(generatedFiles[9], "TestClassArguments = [classArg],");
            AssertFileContains(generatedFiles[9], "TestMethodArguments = [methodArg],");
        });
}