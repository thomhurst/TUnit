using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

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
        async generatedFiles =>
        {
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 1;");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 2;");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 3;");

            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 1;");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg1 = \"String\";");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 2;");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg1 = \"String2\";");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 3;");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg1 = \"String3\";");

            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.TestEnum methodArg = global::TUnit.TestProject.TestEnum.One;");
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.TestEnum methodArg = global::TUnit.TestProject.TestEnum.Two;");
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.TestEnum methodArg = (global::TUnit.TestProject.TestEnum)(-1);");
            
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = null;");
            
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"\";");
            
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"Foo bar!\";");
            
            await AssertFileContains(generatedFiles[0], "global::System.Boolean? methodArg = null;");
            await AssertFileContains(generatedFiles[0], "global::System.Boolean? methodArg = false;");
            await AssertFileContains(generatedFiles[0], "global::System.Boolean? methodArg = true;");

            await AssertFileContains(generatedFiles[0], "global::System.Type methodArg = typeof(global::System.Object);");

            await AssertFileContains(generatedFiles[0], "global::System.Int32[] methodArg = new[] { 1, 2, 3 };");

            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::System.Int32.MaxValue;");
        });
}