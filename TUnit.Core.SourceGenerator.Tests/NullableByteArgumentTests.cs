using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class NullableByteArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NullableByteArgumentTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);

            await AssertFileContains(generatedFiles[0], 
                """
                global::System.Byte? methodArg = (global::System.Byte)1;
                """);
            
            await AssertFileContains(generatedFiles[0], 
                """
                global::System.Byte? methodArg = null;
                """);
            
            await AssertFileContains(generatedFiles[1], 
                """
                global::System.Byte methodArg = (global::System.Byte)1;
                global::System.Byte? methodArg1 = (global::System.Byte)1;
                """);
            
            await AssertFileContains(generatedFiles[1], 
                """
                global::System.Byte methodArg = (global::System.Byte)1;
                global::System.Byte? methodArg1 = null;
                """);
        });
}