using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class TimeoutCancellationTokenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TimeoutCancellationTokenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(7));
            
            AssertFileContains(generatedFiles[0], "TestName = \"BasicTest\"");
            AssertFileContains(generatedFiles[0], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.BasicTest(cancellationToken))");
            
            AssertFileContains(generatedFiles[1], "TestName = \"InheritedTimeoutAttribute\"");
            AssertFileContains(generatedFiles[1], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.InheritedTimeoutAttribute(cancellationToken))");

            AssertFileContains(generatedFiles[2], "TestName = \"DataTest\"");
            AssertFileContains(generatedFiles[2], "global::System.Int32 methodArg = 1;");
            AssertFileContains(generatedFiles[2], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataTest(methodArg, cancellationToken))");
            
            AssertFileContains(generatedFiles[3], "TestName = \"DataSourceTest\"");
            AssertFileContains(generatedFiles[3], "global::System.Int32 methodArg = global::TUnit.TestProject.TimeoutCancellationTokenTests.DataSource();");
            AssertFileContains(generatedFiles[3], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSourceTest(methodArg, cancellationToken))");
            
            AssertFileContains(generatedFiles[4], "TestName = \"MatrixTest\"");
            AssertFileContains(generatedFiles[4], "global::System.Int32 methodArg = 1;");
            AssertFileContains(generatedFiles[4], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.MatrixTest(methodArg, cancellationToken))");
            
            AssertFileContains(generatedFiles[5], "TestName = \"MatrixTest\"");
            AssertFileContains(generatedFiles[5], "global::System.Int32 methodArg = 2;");
            AssertFileContains(generatedFiles[5], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.MatrixTest(methodArg, cancellationToken))");
            
            AssertFileContains(generatedFiles[6], "TestName = \"MatrixTest\"");
            AssertFileContains(generatedFiles[6], "global::System.Int32 methodArg = 3;");
            AssertFileContains(generatedFiles[6], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.MatrixTest(methodArg, cancellationToken))");
        });
}