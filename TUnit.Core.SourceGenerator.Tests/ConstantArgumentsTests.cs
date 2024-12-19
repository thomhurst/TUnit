using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ConstantArgumentsTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ConstantArgumentsTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(7);

            await AssertFileContains(generatedFiles[0], 
                "global::System.String methodArg = \"123\";");
            
            await AssertFileContains(generatedFiles[1], 
                "global::System.Int32 methodArg = 123;");
            
            await AssertFileContains(generatedFiles[2], 
                "global::System.Double methodArg = 1.23;");
            
            await AssertFileContains(generatedFiles[3], 
                "global::System.Single methodArg = 1.23F;");
            
            await AssertFileContains(generatedFiles[4], 
                "global::System.Int64 methodArg = 123L;");
            
            await AssertFileContains(generatedFiles[5], 
                "global::System.UInt32 methodArg = 123U;");
            
            await AssertFileContains(generatedFiles[6], 
                "global::System.UInt64 methodArg = 123UL;");
        });
}