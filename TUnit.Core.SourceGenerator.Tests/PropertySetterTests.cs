using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class PropertySetterTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "PropertySetterTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            
            await AssertFileContains(generatedFiles[0], "global::System.String propertyArg = \"1\";");
            
            await AssertFileContains(generatedFiles[0], "global::System.String propertyArg1 = global::TUnit.TestProject.PropertySetterTests.MethodData();");
            
            await AssertFileContains(generatedFiles[0], "var propertyInfo2 = testClassType.GetProperty(\"Property3\", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);");
            await AssertFileContains(generatedFiles[0], "var propertyDataAttribute2 = propertyInfo2.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.PropertySetterTests.InnerModel>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], "var propertyArg2 = propertyDataAttribute2.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Property,\n   TestClassType = testClassType,\n   ParameterInfos = null,\n   PropertyInfo = propertyInfo2,\n   TestObjectBag = objectBag,\n   TestSessionId = sessionId,\n}).ElementAtOrDefault(0);");
            
            await AssertFileContains(generatedFiles[0], "var propertyInfo3 = testClassType.GetProperty(\"Property4\", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);");
            await AssertFileContains(generatedFiles[0], "var propertyDataAttribute3 = propertyInfo3.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.PropertySetterTests.InnerModel>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], "var propertyArg3 = propertyDataAttribute3.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Property,\n   TestClassType = testClassType,\n   ParameterInfos = null,\n   PropertyInfo = propertyInfo3,\n   TestObjectBag = objectBag,\n   TestSessionId = sessionId,\n}).ElementAtOrDefault(0);");
            
            await AssertFileContains(generatedFiles[0], "var propertyInfo4 = testClassType.GetProperty(\"Property5\", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);");
            await AssertFileContains(generatedFiles[0], "var propertyDataAttribute4 = propertyInfo4.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.PropertySetterTests.InnerModel>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], "var propertyArg4 = propertyDataAttribute4.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Property,\n   TestClassType = testClassType,\n   ParameterInfos = null,\n   PropertyInfo = propertyInfo4,\n   TestObjectBag = objectBag,\n   TestSessionId = sessionId,\n}).ElementAtOrDefault(0);");
            
            await AssertFileContains(generatedFiles[0], "var propertyInfo5 = testClassType.GetProperty(\"Property6\", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);");
            await AssertFileContains(generatedFiles[0], "var propertyDataAttribute5 = propertyInfo5.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.PropertySetterTests.InnerModel>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], "var propertyArg5 = propertyDataAttribute5.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Property,\n   TestClassType = testClassType,\n   ParameterInfos = null,\n   PropertyInfo = propertyInfo5,\n   TestObjectBag = objectBag,\n   TestSessionId = sessionId,\n}).ElementAtOrDefault(0);");
            
            // Stati
            await AssertFileContains(generatedFiles[0], "var propertyInfo6 = testClassType.GetProperty(\"StaticProperty\", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);");
            await AssertFileContains(generatedFiles[0], "var propertyDataAttribute6 = propertyInfo6.GetCustomAttributes<global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.PropertySetterTests.StaticInnerModel>>(true).ElementAt(0);");
            await AssertFileContains(generatedFiles[0], "var propertyArg6 = propertyDataAttribute6.GenerateDataSources(new DataGeneratorMetadata\n{\n   Type = TUnit.Core.Enums.DataGeneratorType.Property,\n   TestClassType = testClassType,\n   ParameterInfos = null,\n   PropertyInfo = propertyInfo6,\n   TestObjectBag = objectBag,\n   TestSessionId = sessionId,\n}).ElementAtOrDefault(0);");
            await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.PropertySetterTests.StaticProperty = propertyArg6;");

            await AssertFileContains(generatedFiles[0], "Property1 = propertyArg,");
            await AssertFileContains(generatedFiles[0], "Property2 = propertyArg1,");
            await AssertFileContains(generatedFiles[0], "Property3 = propertyArg2,");
            await AssertFileContains(generatedFiles[0], "Property4 = propertyArg3,");
            await AssertFileContains(generatedFiles[0], "Property5 = propertyArg4,");
            await AssertFileContains(generatedFiles[0], "Property6 = propertyArg5,");
            
            await AssertFileContains(generatedFiles[0], "TestClassProperties = [propertyArg, propertyArg1, propertyArg2, propertyArg3, propertyArg4, propertyArg5],");
        });
}