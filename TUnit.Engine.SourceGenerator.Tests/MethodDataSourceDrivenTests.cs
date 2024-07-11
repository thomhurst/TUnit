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
            
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg0 = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Method(methodArg0)"));
            Assert.That(generatedFiles[0], Does.Contain(
                """
                				InternalTestMethodArguments = [new TestData(methodArg0, typeof(global::System.Int32), InjectedDataType.None)
                				{
                    				DisposeAfterTest = true,
                				}],
                """));
            
            Assert.That(generatedFiles[1], Does.Contain("global::System.Int32 methodArg0 = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Method2(methodArg0)"));
            Assert.That(generatedFiles[1], Does.Contain(
	            """
	            				InternalTestMethodArguments = [new TestData(methodArg0, typeof(global::System.Int32), InjectedDataType.None)
	            				{
	                				DisposeAfterTest = false,
	            				}],
	            """));
        });
}