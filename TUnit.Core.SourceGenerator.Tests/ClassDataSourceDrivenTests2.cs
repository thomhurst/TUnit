using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

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
            await AssertFileContains(generatedFiles[0], "var classArgGeneratedDataArray = classDataAttribute.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   TestClassType = testClassType,\n   ParameterInfos = typeof(global::TUnit.TestProject.ClassDataSourceDrivenTests2).GetConstructors().First().GetParameters(),\n   PropertyInfo = null,\n   TestObjectBag = objectBag,\n   TestSessionId = sessionId,\n});");
            await AssertFileContains(generatedFiles[0],
                "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArgGeneratedData), sessionId);");
        });
}