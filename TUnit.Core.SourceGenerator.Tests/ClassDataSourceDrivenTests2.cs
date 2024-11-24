using TUnit.Assertions.Assertions.Collections;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTests2 : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests2.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(2);

            await AssertFileContains(generatedFiles[0], "var classDataAttribute = typeof(global::TUnit.TestProject.ClassDataSourceDrivenTests2).GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], """
                                                        var classArgDataGeneratorMetadata = new DataGeneratorMetadata
                                                        {
                                                           Type = TUnit.Core.Enums.DataGeneratorType.Parameters,
                                                           TestClassType = testClassType,
                                                           ParameterInfos = typeof(global::TUnit.TestProject.ClassDataSourceDrivenTests2).GetConstructors().First().GetParameters(),
                                                           PropertyInfo = null,
                                                           TestBuilderContext = testBuilderContextAccessor,
                                                           TestSessionId = sessionId,
                                                        };
                                                        """);
            await AssertFileContains(generatedFiles[0], "var classArgGeneratedDataArray = classDataAttribute.GenerateDataSources(classArgDataGeneratorMetadata);");
            await AssertFileContains(generatedFiles[0],
                "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArgGeneratedData), sessionId, testBuilderContext);");
        });
}