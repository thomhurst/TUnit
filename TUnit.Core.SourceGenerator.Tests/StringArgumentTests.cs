using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class StringArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "StringArgumentTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);

            await AssertFileContains(generatedFiles[0], 
                """
                global::System.String methodArg = "";
                """);
            
            await AssertFileContains(generatedFiles[0], 
                """
                global::System.String methodArg = @"\";
                """);
            
            await AssertFileContains(generatedFiles[0], 
                """
                global::System.String methodArg = @"\t";
                """);
            
            await AssertFileContains(generatedFiles[0], 
                """
                global::System.String methodArg = "\t";
                """);
            
            await AssertFileContains(generatedFiles[0], 
                """
                global::System.String methodArg = "\\t";
                """);
            
            await AssertFileContains(generatedFiles[0], 
                """
                global::System.String methodArg = "\\\t";
                """);
            
            await AssertFileContains(generatedFiles[0], 
                """
                global::System.String methodArg = "\\\\t";
                """);

            await AssertFileContains(generatedFiles[0], 
                """"
                global::System.String methodArg = """
                        Hello
                        World
                        """;
                """");
        });
}