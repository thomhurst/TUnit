using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

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
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(9));
            
            AssertFileContains(generatedFiles[0], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();");
            AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();");
            
            AssertFileContains(generatedFiles[1], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();");
            AssertFileContains(generatedFiles[1], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();");
            
            AssertFileContains(generatedFiles[2], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();");
            AssertFileContains(generatedFiles[2], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();");
            
            AssertFileContains(generatedFiles[3], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();");
            AssertFileContains(generatedFiles[3], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();");
            
            AssertFileContains(generatedFiles[4], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();");
            AssertFileContains(generatedFiles[4], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();");
            
            AssertFileContains(generatedFiles[5], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();");
            AssertFileContains(generatedFiles[5], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();");
            
            AssertFileContains(generatedFiles[6], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();");
            AssertFileContains(generatedFiles[6], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();");
            
            AssertFileContains(generatedFiles[7], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();");
            AssertFileContains(generatedFiles[7], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();");
            
            AssertFileContains(generatedFiles[8], "global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();");
            AssertFileContains(generatedFiles[8], "global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();");
        });
}