using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class MethodDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();");
            await AssertFileContains(generatedFiles[0], "classInstance.DataSource_Method(methodArg)");
            await AssertFileContains(generatedFiles[0], 
                """
                                InternalTestMethodArguments = 
                                [
                                    new TestData(methodArg, typeof(global::System.Int32), InjectedDataType.None)
                                    {
                                        DisposeAfterTest = true,
                                    },
                                ],
                """);
            
            await AssertFileContains(generatedFiles[1], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();");
            await AssertFileContains(generatedFiles[1], "classInstance.DataSource_Method2(methodArg)");
            await AssertFileContains(generatedFiles[1], 
                """
                                InternalTestMethodArguments = 
                                [
                                    new TestData(methodArg, typeof(global::System.Int32), InjectedDataType.None)
                                    {
                                        DisposeAfterTest = false,
                                    },
                                ],
                """);
        });
}