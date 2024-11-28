using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class MethodDataSourceDrivenWithCancellationTokenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenWithCancellationTokenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenWithCancellationTokenTests.T();");
            await AssertFileContains(generatedFiles[0], "classInstance.MyTest(methodArg, cancellationToken)");

            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = global::TUnit.TestProject.MethodDataSourceDrivenWithCancellationTokenTests.FuncT()();");
            await AssertFileContains(generatedFiles[0], "classInstance.MyTest(methodArg, cancellationToken)");
            
            await AssertFileContains(generatedFiles[0], "foreach (var methodDataAccessor in global::TUnit.TestProject.MethodDataSourceDrivenWithCancellationTokenTests.EnumerableT())");
            await AssertFileContains(generatedFiles[0], "var methodData = methodDataAccessor;");
            await AssertFileContains(generatedFiles[0], "classInstance.MyTest(methodData, cancellationToken)");

            await AssertFileContains(generatedFiles[0], "foreach (var methodDataAccessor in global::TUnit.TestProject.MethodDataSourceDrivenWithCancellationTokenTests.EnumerableFuncT())");
            await AssertFileContains(generatedFiles[0], "var methodData = methodDataAccessor();");
            await AssertFileContains(generatedFiles[0], "classInstance.MyTest(methodData, cancellationToken)");
            
            await AssertFileContains(generatedFiles[0], "foreach (var methodDataAccessor in global::TUnit.TestProject.MethodDataSourceDrivenWithCancellationTokenTests.ArrayT())");
            await AssertFileContains(generatedFiles[0], "var methodData = methodDataAccessor;");
            await AssertFileContains(generatedFiles[0], "classInstance.MyTest(methodData, cancellationToken)");

            await AssertFileContains(generatedFiles[0], "foreach (var methodDataAccessor in global::TUnit.TestProject.MethodDataSourceDrivenWithCancellationTokenTests.ArrayFuncT())");
            await AssertFileContains(generatedFiles[0], "var methodData = methodDataAccessor();");
            await AssertFileContains(generatedFiles[0], "classInstance.MyTest(methodData, cancellationToken)");
        });
}