using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class PropertySetterTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "PropertySetterTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(1));
            
            AssertFileContains(generatedFiles[0], "global::System.String propertyArg = \"1\";");
            AssertFileContains(generatedFiles[0], "global::System.String propertyArg1 = global::TUnit.TestProject.PropertySetterTests.MethodData();");
            AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.PropertySetterTests.InnerModel propertyArg2 = new global::TUnit.TestProject.PropertySetterTests.InnerModel();");
            AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.PropertySetterTests.InnerModel propertyArg3 = TestDataContainer.GetGlobalInstance<global::TUnit.TestProject.PropertySetterTests.InnerModel>(() => new global::TUnit.TestProject.PropertySetterTests.InnerModel());");
            AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.PropertySetterTests.InnerModel propertyArg4 = TestDataContainer.GetInstanceForType<global::TUnit.TestProject.PropertySetterTests.InnerModel>(typeof(global::TUnit.TestProject.PropertySetterTests), () => new global::TUnit.TestProject.PropertySetterTests.InnerModel());");
            AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.PropertySetterTests.InnerModel propertyArg5 = TestDataContainer.GetInstanceForKey<global::TUnit.TestProject.PropertySetterTests.InnerModel>(\"Key\", () => new global::TUnit.TestProject.PropertySetterTests.InnerModel());");
            
            AssertFileContains(generatedFiles[0], "Property1 = propertyArg,");
            AssertFileContains(generatedFiles[0], "Property2 = propertyArg1,");
            AssertFileContains(generatedFiles[0], "Property3 = propertyArg2,");
            AssertFileContains(generatedFiles[0], "Property4 = propertyArg3,");
            AssertFileContains(generatedFiles[0], "Property5 = propertyArg4,");
            AssertFileContains(generatedFiles[0], "Property6 = propertyArg5,");
            
            AssertFileContains(generatedFiles[0], "TestClassProperties = [propertyArg, propertyArg1, propertyArg2, propertyArg3, propertyArg4, propertyArg5],");
            
            AssertFileContains(generatedFiles[0], "new TestData(propertyArg, typeof(global::System.String), InjectedDataType.None) { DisposeAfterTest = true, }");
            AssertFileContains(generatedFiles[0], "new TestData(propertyArg1, typeof(global::System.String), InjectedDataType.None) { DisposeAfterTest = true, }");
            AssertFileContains(generatedFiles[0], "new TestData(propertyArg2, typeof(global::TUnit.TestProject.PropertySetterTests.InnerModel), InjectedDataType.None) { DisposeAfterTest = true, }");
            AssertFileContains(generatedFiles[0], "new TestData(propertyArg3, typeof(global::TUnit.TestProject.PropertySetterTests.InnerModel), InjectedDataType.SharedGlobally),");
            AssertFileContains(generatedFiles[0], "new TestData(propertyArg4, typeof(global::TUnit.TestProject.PropertySetterTests.InnerModel), InjectedDataType.SharedByTestClassType)");
            AssertFileContains(generatedFiles[0], "new TestData(propertyArg5, typeof(global::TUnit.TestProject.PropertySetterTests.InnerModel), InjectedDataType.SharedByKey) { StringKey = \"Key\" }");
        });
}