using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "Dummy",
                    "SomeAsyncDisposableClass.cs")
            ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);

            await AssertFileContains(generatedFiles[0], "var methodDataAttribute = methodInfo.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], "var methodArgGeneratedDataArray = methodDataAttribute.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   TestClassType = testClassType,\n   ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null,\n   TestObjectBag = objectBag,\n   TestSessionId = sessionId,\n});");
            await AssertFileContains(generatedFiles[0], "classInstance.DataSource_Class(methodArgGeneratedData)");

            await AssertFileContains(generatedFiles[1], "var methodDataAttribute = methodInfo.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[1], "var methodArgGeneratedDataArray = methodDataAttribute.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   TestClassType = testClassType,\n   ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null,\n   TestObjectBag = objectBag,\n   TestSessionId = sessionId,\n});");
            await AssertFileContains(generatedFiles[1], "classInstance.DataSource_Class_Generic(methodArgGeneratedData)");
        });
}