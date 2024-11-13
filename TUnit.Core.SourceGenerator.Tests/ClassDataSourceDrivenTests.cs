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
            await Assert.That(generatedFiles.Length).IsEqualTo(7);

            await AssertFileContains(generatedFiles[0], "var methodDataAttribute = methodInfo.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], """
                                                        var methodArgDataGeneratorMetadata = new DataGeneratorMetadata
                                                        {
                                                           Type = TUnit.Core.Enums.DataGeneratorType.Parameters,
                                                           TestClassType = testClassType,
                                                           ParameterInfos = methodInfo.GetParameters(),
                                                           PropertyInfo = null,
                                                           TestBuilderContext = testBuilderContextAccessor,
                                                           TestSessionId = sessionId,
                                                        };
                                                        """);
            await AssertFileContains(generatedFiles[0], "var methodArgGeneratedDataArray = methodDataAttribute.GenerateDataSources(methodArgDataGeneratorMetadata).ToUniqueElementsEnumerable();");
            await AssertFileContains(generatedFiles[0], "classInstance.DataSource_Class(methodArgGeneratedData)");

            await AssertFileContains(generatedFiles[1], "var methodDataAttribute = methodInfo.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.Dummy.SomeAsyncDisposableClass>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[1], """
                                                        var methodArgDataGeneratorMetadata = new DataGeneratorMetadata
                                                        {
                                                           Type = TUnit.Core.Enums.DataGeneratorType.Parameters,
                                                           TestClassType = testClassType,
                                                           ParameterInfos = methodInfo.GetParameters(),
                                                           PropertyInfo = null,
                                                           TestBuilderContext = testBuilderContextAccessor,
                                                           TestSessionId = sessionId,
                                                        };
                                                        """);
            await AssertFileContains(generatedFiles[1], "var methodArgGeneratedDataArray = methodDataAttribute.GenerateDataSources(methodArgDataGeneratorMetadata).ToUniqueElementsEnumerable();");
            await AssertFileContains(generatedFiles[1], "classInstance.DataSource_Class_Generic(methodArgGeneratedData)");
        });
}