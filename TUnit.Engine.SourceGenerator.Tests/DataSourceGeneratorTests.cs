using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class DataSourceGeneratorTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DataSourceGeneratorTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(6);
            
            await AssertFileContains(generatedFiles[0], "TestId = $\"global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32, global::System.String, global::System.Boolean>:{classDataIndex}:CL-GAC0:global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32>:{testMethodDataIndex}:TL-GAC0:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method(System.Int32):0\"");
            await AssertFileContains(generatedFiles[0], "var methodDataAttribute = methodInfo.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], "var methodArgGeneratedDataArray = methodDataAttribute.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   TestClassType = testClassType, ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null, TestObjectBag = objectBag,\n});");
            await AssertFileContains(generatedFiles[0], "foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)");
            await AssertFileContains(generatedFiles[0], "classInstance.GeneratedData_Method(methodArgGeneratedData))");

            await AssertFileContains(generatedFiles[2], "TestId = $\"global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32, global::System.String, global::System.Boolean>:{classDataIndex}:CL-GAC0:global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32, global::System.String, global::System.Boolean>:{testMethodDataIndex}:TL-GAC0:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method2(System.Int32,System.String,System.Boolean):0\",");
            await AssertFileContains(generatedFiles[2], "var methodDataAttribute = methodInfo.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32, global::System.String, global::System.Boolean>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[2], "var methodArgGeneratedDataArray = methodDataAttribute.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n  TestClassType = testClassType, ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null, TestObjectBag = objectBag,\n});");
            await AssertFileContains(generatedFiles[2], "foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)");
            await AssertFileContains(generatedFiles[2], "global::System.Int32 methodArg = methodArgGeneratedData.Item1;");
            await AssertFileContains(generatedFiles[2], "global::System.String methodArg1 = methodArgGeneratedData.Item2;");
            await AssertFileContains(generatedFiles[2], "global::System.Boolean methodArg2 = methodArgGeneratedData.Item3;");
            await AssertFileContains(generatedFiles[2], "classInstance.GeneratedData_Method2(methodArg, methodArg1, methodArg2)");
            
            await AssertFileContains(generatedFiles[4], "TestId = $\"global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32, global::System.String, global::System.Boolean>:{classDataIndex}:CL-GAC0:global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute:{testMethodDataIndex}:TL-GAC0:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method3(System.Int32,System.String,System.Boolean):0\",");
            await AssertFileContains(generatedFiles[4], "var methodDataAttribute = methodInfo.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[4], "var methodArgGeneratedDataArray = methodDataAttribute.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n  TestClassType = testClassType, ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null, TestObjectBag = objectBag,\n});");
            await AssertFileContains(generatedFiles[4], "foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)");
            await AssertFileContains(generatedFiles[4], "global::System.Int32 methodArg = methodArgGeneratedData.Item1;");
            await AssertFileContains(generatedFiles[4], "global::System.String methodArg1 = methodArgGeneratedData.Item2;");
            await AssertFileContains(generatedFiles[4], "global::System.Boolean methodArg2 = methodArgGeneratedData.Item3;");
            await AssertFileContains(generatedFiles[4], "classInstance.GeneratedData_Method3(methodArg, methodArg1, methodArg2)");
        });
}