using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class StringArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "StringArgumentTests.cs"),
        async generatedFiles =>
        {
            await AssertFileContains(generatedFiles[0], 
                """
                global::System.String methodArg = "";
                """);
            
            await AssertFileContains(generatedFiles[1], 
                """
                global::System.String methodArg = @"\";
                """);
            
            await AssertFileContains(generatedFiles[2], 
                """
                global::System.String methodArg = @"\t";
                """);
            
            await AssertFileContains(generatedFiles[3], 
                """
                global::System.String methodArg = "\t";
                """);
            
            await AssertFileContains(generatedFiles[4], 
                """
                global::System.String methodArg = "\\t";
                """);
            
            await AssertFileContains(generatedFiles[5], 
                """
                global::System.String methodArg = "\\\t";
                """);
            
            await AssertFileContains(generatedFiles[6], 
                """
                global::System.String methodArg = "\\\\t";
                """);

            await AssertFileContains(generatedFiles[7], 
                """"
                global::System.String methodArg = """
                        Hello
                        World
                        """;
                """");
        });
}