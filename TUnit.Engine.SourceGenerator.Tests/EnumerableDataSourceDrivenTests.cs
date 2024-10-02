using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class EnumerableDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "EnumerableDataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("foreach (var methodData in global::TUnit.TestProject.EnumerableDataSourceDrivenTests.SomeMethod())"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodData],"));
            Assert.That(generatedFiles[0], Does.Contain(
                """
                				InternalTestMethodArguments = [new TestData(methodData, typeof(global::System.Collections.Generic.IEnumerable<global::System.Int32>), InjectedDataType.None)
                				{
                    				DisposeAfterTest = true,
                				}],
                """));
            
            Assert.That(generatedFiles[1], Does.Contain("foreach (var methodData in global::TUnit.TestProject.EnumerableDataSourceDrivenTests.SomeMethod())"));
            Assert.That(generatedFiles[1], Does.Contain("TestMethodArguments = [methodData],"));
            Assert.That(generatedFiles[1], Does.Contain(
	            """
	            				InternalTestMethodArguments = [new TestData(methodData, typeof(global::System.Collections.Generic.IEnumerable<global::System.Int32>), InjectedDataType.None)
	            				{
	                				DisposeAfterTest = false,
	            				}],
	            """));
        });
}