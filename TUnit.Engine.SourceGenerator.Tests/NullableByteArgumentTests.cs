using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class NullableByteArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NullableByteArgumentTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(4);

            await AssertFileContains(generatedFiles[0], 
                """
                global::System.Byte? methodArg = (global::System.Byte)1;
                """);
            
            await AssertFileContains(generatedFiles[1], 
                """
                global::System.Byte? methodArg = null;
                """);
            
            await AssertFileContains(generatedFiles[2], 
                """
                global::System.Byte methodArg = (global::System.Byte)1;
                global::System.Byte? methodArg1 = (global::System.Byte)1;
                """);
            
            await AssertFileContains(generatedFiles[3], 
                """
                global::System.Byte methodArg = (global::System.Byte)1;
                global::System.Byte? methodArg1 = null;
                """);
        });
}