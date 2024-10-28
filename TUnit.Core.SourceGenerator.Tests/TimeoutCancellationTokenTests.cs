using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class TimeoutCancellationTokenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TimeoutCancellationTokenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(6);

            await AssertFileContains(generatedFiles[0], "TestName = \"DefaultTest\"");
            await AssertFileContains(generatedFiles[0], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DefaultTest(cancellationToken))");

            await AssertFileContains(generatedFiles[1], "TestName = \"BasicTest\"");
            await AssertFileContains(generatedFiles[1], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.BasicTest(cancellationToken))");
            
            await AssertFileContains(generatedFiles[2], "TestName = \"InheritedTimeoutAttribute\"");
            await AssertFileContains(generatedFiles[2], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.InheritedTimeoutAttribute(cancellationToken))");

            await AssertFileContains(generatedFiles[3], "TestName = \"DataTest\"");
            await AssertFileContains(generatedFiles[3], "global::System.Int32 methodArg = 1;");
            await AssertFileContains(generatedFiles[3], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataTest(methodArg, cancellationToken))");
            
            await AssertFileContains(generatedFiles[4], "TestName = \"DataSourceTest\"");
            await AssertFileContains(generatedFiles[4], "global::System.Int32 methodArg = global::TUnit.TestProject.TimeoutCancellationTokenTests.DataSource();");
            await AssertFileContains(generatedFiles[4], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSourceTest(methodArg, cancellationToken))");
            
            await AssertFileContains(generatedFiles[5], "TestName = \"MatrixTest\"");
            await AssertFileContains(generatedFiles[5], "global::System.Int32 methodArg = 1;");
            await AssertFileContains(generatedFiles[5], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.MatrixTest(methodArg, cancellationToken))");
            
            await AssertFileContains(generatedFiles[5], "TestName = \"MatrixTest\"");
            await AssertFileContains(generatedFiles[5], "global::System.Int32 methodArg = 2;");
            await AssertFileContains(generatedFiles[5], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.MatrixTest(methodArg, cancellationToken))");
            
            await AssertFileContains(generatedFiles[5], "TestName = \"MatrixTest\"");
            await AssertFileContains(generatedFiles[5], "global::System.Int32 methodArg = 3;");
            await AssertFileContains(generatedFiles[5], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.MatrixTest(methodArg, cancellationToken))");
        });
}