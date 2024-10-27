using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class DataSourceClassCombinedWithDataSourceMethodTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DataSourceClassCombinedWithDataSourceMethod.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "CommonTestData.cs"),
            ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();");
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();");
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();");
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();");
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();");
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();");
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();");
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();");
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();");
        });
}