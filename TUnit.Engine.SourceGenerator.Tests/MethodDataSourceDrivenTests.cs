using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class MethodDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();");
            AssertFileContains(generatedFiles[0], "classInstance.DataSource_Method(methodArg)");
            Assert.That(generatedFiles[0], Does.Contain(
                """
                				InternalTestMethodArguments = [new TestData(methodArg, typeof(global::System.Int32), InjectedDataType.None)
                				{
                    				DisposeAfterTest = true,
                				}],
                """));
            
            AssertFileContains(generatedFiles[1], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();");
            AssertFileContains(generatedFiles[1], "classInstance.DataSource_Method2(methodArg)");
            Assert.That(generatedFiles[1], Does.Contain(
	            """
	            				InternalTestMethodArguments = [new TestData(methodArg, typeof(global::System.Int32), InjectedDataType.None)
	            				{
	                				DisposeAfterTest = false,
	            				}],
	            """));
        });
}