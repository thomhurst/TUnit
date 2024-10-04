using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class DataSourceGeneratorTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DataSourceGeneratorTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(6));
            
            AssertFileContains(generatedFiles[0], "TestId = $\"CL-GAC0:TL-GAC1:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method(System.Int32):0\",");
            AssertFileContains(generatedFiles[0], "var methodArgGeneratedDataArray = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32>>(methodInfo).SelectMany(x => x.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null\n}));");
            AssertFileContains(generatedFiles[0], "foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)");
            AssertFileContains(generatedFiles[0], "classInstance.GeneratedData_Method(methodArgGeneratedData))");

            AssertFileContains(generatedFiles[2], "TestId = $\"CL-GAC0:TL-GAC1:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method2(System.Int32,System.String,System.Boolean):0\",");
            AssertFileContains(generatedFiles[2], "var methodArgGeneratedDataArray = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32, global::System.String, global::System.Boolean>>(methodInfo).SelectMany(x => x.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null\n}));");
            AssertFileContains(generatedFiles[2], "foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)");
            AssertFileContains(generatedFiles[2], "global::System.Int32 methodArg = methodArgGeneratedData.Item1;");
            AssertFileContains(generatedFiles[2], "global::System.String methodArg1 = methodArgGeneratedData.Item2;");
            AssertFileContains(generatedFiles[2], "global::System.Boolean methodArg2 = methodArgGeneratedData.Item3;");
            AssertFileContains(generatedFiles[2], "classInstance.GeneratedData_Method2(methodArg, methodArg1, methodArg2)");
            
            AssertFileContains(generatedFiles[4], "TestId = $\"CL-GAC0:TL-GAC1:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method3(System.Int32,System.String,System.Boolean):0\",");
            AssertFileContains(generatedFiles[4], "var methodArgGeneratedDataArray = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute>(methodInfo).SelectMany(x => x.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null\n}));");
            AssertFileContains(generatedFiles[4], "foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)");
            AssertFileContains(generatedFiles[4], "global::System.Int32 methodArg = methodArgGeneratedData.Item1;");
            AssertFileContains(generatedFiles[4], "global::System.String methodArg1 = methodArgGeneratedData.Item2;");
            AssertFileContains(generatedFiles[4], "global::System.Boolean methodArg2 = methodArgGeneratedData.Item3;");
            AssertFileContains(generatedFiles[4], "classInstance.GeneratedData_Method3(methodArg, methodArg1, methodArg2)");
        });
}