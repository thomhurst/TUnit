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
            
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();"));
            
            Assert.That(generatedFiles[1], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();"));
            Assert.That(generatedFiles[1], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();"));
            
            Assert.That(generatedFiles[2], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();"));
            Assert.That(generatedFiles[2], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.One();"));
            
            Assert.That(generatedFiles[3], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();"));
            Assert.That(generatedFiles[3], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();"));
            
            Assert.That(generatedFiles[4], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();"));
            Assert.That(generatedFiles[4], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();"));
            
            Assert.That(generatedFiles[5], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();"));
            Assert.That(generatedFiles[5], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Two();"));
            
            Assert.That(generatedFiles[6], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.One();"));
            Assert.That(generatedFiles[6], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();"));
            
            Assert.That(generatedFiles[7], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Two();"));
            Assert.That(generatedFiles[7], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();"));
            
            Assert.That(generatedFiles[8], Does.Contain("global::System.Int32 classArg = global::TUnit.TestProject.CommonTestData.Three();"));
            Assert.That(generatedFiles[8], Does.Contain("global::System.Int32 methodArg = global::TUnit.TestProject.CommonTestData.Three();"));
        });
}