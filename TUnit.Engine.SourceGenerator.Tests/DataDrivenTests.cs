using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class DataDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DataDrivenTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "TestEnum.cs")
            ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(18));

            AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 1;");
            AssertFileContains(generatedFiles[1], "global::System.Int32 methodArg = 2;");
            AssertFileContains(generatedFiles[2], "global::System.Int32 methodArg = 3;");

            AssertFileContains(generatedFiles[3], "global::System.Int32 methodArg = 1;");
            AssertFileContains(generatedFiles[3], "global::System.String methodArg1 = \"String\";");
            AssertFileContains(generatedFiles[4], "global::System.Int32 methodArg = 2;");
            AssertFileContains(generatedFiles[4], "global::System.String methodArg1 = \"String2\";");
            AssertFileContains(generatedFiles[5], "global::System.Int32 methodArg = 3;");
            AssertFileContains(generatedFiles[5], "global::System.String methodArg1 = \"String3\";");

            AssertFileContains(generatedFiles[6], "global::TUnit.TestProject.TestEnum methodArg = global::TUnit.TestProject.TestEnum.One;");
            AssertFileContains(generatedFiles[7], "global::TUnit.TestProject.TestEnum methodArg = global::TUnit.TestProject.TestEnum.Two;");
            AssertFileContains(generatedFiles[8], "global::TUnit.TestProject.TestEnum methodArg = (global::TUnit.TestProject.TestEnum)(-1);");
            
            AssertFileContains(generatedFiles[9], "global::System.String methodArg = null;");
            
            AssertFileContains(generatedFiles[10], "global::System.String methodArg = \"\";");
            
            AssertFileContains(generatedFiles[11], "global::System.String methodArg = \"Foo bar!\";");
            
            AssertFileContains(generatedFiles[12], "global::System.Boolean? methodArg = null;");
            AssertFileContains(generatedFiles[13], "global::System.Boolean? methodArg = false;");
            AssertFileContains(generatedFiles[14], "global::System.Boolean? methodArg = true;");

            AssertFileContains(generatedFiles[15], "global::System.Type methodArg = typeof(global::System.Object);");

            AssertFileContains(generatedFiles[16], "global::System.Int32[] methodArg = new[] { 1, 2, 3 };");

            AssertFileContains(generatedFiles[17], "global::System.Int32 methodArg = global::System.Int32.MaxValue;");
        });
}