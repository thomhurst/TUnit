using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.UnitTests;

public class StaticPropertyDataSourceTests
{
    // Static property with Arguments attribute
    [Arguments("static injected value")]
    public static string? StaticStringProperty { get; set; }
    
    // Static property with MethodDataSource
    [MethodDataSource(nameof(GetStaticTestData))]
    public static TestData? StaticDataProperty { get; set; }
    
    // Static property with ClassDataSource
    [ClassDataSource<StaticTestDataProvider>]
    public static IStaticTestDataProvider? StaticDataProviderProperty { get; set; }
    
    [Test]
    public async Task StaticPropertyInjection_ArgumentsAttribute_InjectsValue()
    {
        await Assert.That(StaticStringProperty).IsNotNull();
        await Assert.That(StaticStringProperty).IsEqualTo("static injected value");
    }
    
    [Test]
    public async Task StaticPropertyInjection_MethodDataSource_InjectsValue()
    {
        await Assert.That(StaticDataProperty).IsNotNull();
        await Assert.That(StaticDataProperty!.Value).IsEqualTo("static test data");
    }
    
    [Test]
    public async Task StaticPropertyInjection_ClassDataSource_InjectsAndInitializes()
    {
        await Assert.That(StaticDataProviderProperty).IsNotNull();
        await Assert.That(StaticDataProviderProperty!.IsInitialized).IsTrue();
        await Assert.That(StaticDataProviderProperty.GetData()).IsEqualTo("static initialized data");
    }
    
    // Helper methods and types
    public static TestData GetStaticTestData()
    {
        return new TestData { Value = "static test data" };
    }
    
    public class TestData
    {
        public string Value { get; set; } = "";
    }
    
    public interface IStaticTestDataProvider
    {
        bool IsInitialized { get; }
        string GetData();
    }
    
    public class StaticTestDataProvider : IStaticTestDataProvider, IAsyncInitializer
    {
        public bool IsInitialized { get; private set; }
        
        public async Task InitializeAsync()
        {
            await Task.Delay(1);
            IsInitialized = true;
        }
        
        public string GetData()
        {
            return IsInitialized ? "static initialized data" : "not initialized";
        }
    }
}

// Test for inheritance of static properties
public class BaseStaticClass
{
    [Arguments("base static value")]
    public static string? BaseStaticProperty { get; set; }
}

public class DerivedStaticPropertyTests : BaseStaticClass
{
    [Arguments("derived static value")]
    public static string? DerivedStaticProperty { get; set; }
    
    [Test]
    public async Task StaticPropertyInjection_Inheritance_InjectsBaseAndDerivedProperties()
    {
        await Assert.That(BaseStaticProperty).IsEqualTo("base static value");
        await Assert.That(DerivedStaticProperty).IsEqualTo("derived static value");
    }
}