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
            
            Assert.That(generatedFiles[0], Does.Contain("TestId = $\"CL-GAC0:TL-GAC1:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method(System.Int32):0\","));
            AssertFileContains(generatedFiles[0], "var methodArgGeneratedDataArray = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32>>(methodInfo).SelectMany(x => x.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null\n}));".IgnoreWhitespaceFormatting());
            Assert.That(generatedFiles[0], Does.Contain("foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.GeneratedData_Method(methodArgGeneratedData))"));

            Assert.That(generatedFiles[2], Does.Contain("TestId = $\"CL-GAC0:TL-GAC1:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method2(System.Int32,System.String,System.Boolean):0\","));
            Assert.That(generatedFiles[2], Does.Contain("var methodArgGeneratedDataArray = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32>>(methodInfo).SelectMany(x => x.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null\n}));"));
            Assert.That(generatedFiles[2], Does.Contain("foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)"));
            Assert.That(generatedFiles[2], Does.Contain("global::System.Int32 methodArg = methodArgGeneratedData.Item1;"));
            Assert.That(generatedFiles[2], Does.Contain("global::System.String methodArg1 = methodArgGeneratedData.Item2;"));
            Assert.That(generatedFiles[2], Does.Contain("global::System.Boolean methodArg2 = methodArgGeneratedData.Item3;"));
            Assert.That(generatedFiles[2], Does.Contain("classInstance.GeneratedData_Method2(methodArg, methodArg1, methodArg2)"));
            
            Assert.That(generatedFiles[4], Does.Contain("TestId = $\"CL-GAC0:TL-GAC1:TUnit.TestProject.DataSourceGeneratorTests(System.Int32,System.String,System.Boolean).GeneratedData_Method3(System.Int32,System.String,System.Boolean):0\","));
            Assert.That(generatedFiles[4], Does.Contain("var methodArgGeneratedDataArray = global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<global::TUnit.TestProject.DataSourceGeneratorTests.AutoFixtureGeneratorAttribute<global::System.Int32>>(methodInfo).SelectMany(x => x.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Parameters,\n   ParameterInfos = methodInfo.GetParameters(),\n   PropertyInfo = null\n}));"));
            Assert.That(generatedFiles[4], Does.Contain("foreach (var methodArgGeneratedData in methodArgGeneratedDataArray)"));
            Assert.That(generatedFiles[4], Does.Contain("global::System.Int32 methodArg = methodArgGeneratedData.Item1;"));
            Assert.That(generatedFiles[4], Does.Contain("global::System.String methodArg1 = methodArgGeneratedData.Item2;"));
            Assert.That(generatedFiles[4], Does.Contain("global::System.Boolean methodArg2 = methodArgGeneratedData.Item3;"));
            Assert.That(generatedFiles[4], Does.Contain("classInstance.GeneratedData_Method3(methodArg, methodArg1, methodArg2)"));
        });
}