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
            Assert.That(generatedFiles.Length, Is.EqualTo(13));

            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg0 = 1;"));
            Assert.That(generatedFiles[1], Does.Contain("global::System.Int32 methodArg0 = 2;"));
            Assert.That(generatedFiles[2], Does.Contain("global::System.Int32 methodArg0 = 3;"));

            Assert.That(generatedFiles[3], Does.Contain("global::System.Int32 methodArg0 = 1;"));
            Assert.That(generatedFiles[3], Does.Contain("global::System.String methodArg1 = \"String\";"));
            Assert.That(generatedFiles[4], Does.Contain("global::System.Int32 methodArg0 = 2;"));
            Assert.That(generatedFiles[4], Does.Contain("global::System.String methodArg1 = \"String2\";"));
            Assert.That(generatedFiles[5], Does.Contain("global::System.Int32 methodArg0 = 3;"));
            Assert.That(generatedFiles[5], Does.Contain("global::System.String methodArg1 = \"String3\";"));

            Assert.That(generatedFiles[6], Does.Contain("global::TUnit.TestProject.TestEnum methodArg0 = (global::TUnit.TestProject.TestEnum) 0;"));
            Assert.That(generatedFiles[7], Does.Contain("global::TUnit.TestProject.TestEnum methodArg0 = (global::TUnit.TestProject.TestEnum) 1;"));
            
            Assert.That(generatedFiles[8], Does.Contain("global::System.String methodArg0 = null;"));
            
            Assert.That(generatedFiles[9], Does.Contain("global::System.String methodArg0 = \"\";"));
            
            Assert.That(generatedFiles[10], Does.Contain("global::System.String methodArg0 = \"Foo bar!\";"));
            
            Assert.That(generatedFiles[11], Does.Contain("global::System.Boolean methodArg0 = false;"));
            Assert.That(generatedFiles[12], Does.Contain("global::System.Boolean methodArg0 = true;"));

        });
}