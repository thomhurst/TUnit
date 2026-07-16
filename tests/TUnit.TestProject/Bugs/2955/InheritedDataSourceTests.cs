using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2955;

// Reproducing the issue from GitHub issue #2955
// https://github.com/thomhurst/TUnit/issues/2955

public class Data1 : IAsyncInitializer
{
    public string Value { get; set; } = string.Empty;

    public Task InitializeAsync()
    {
        Value = "Data1 Initialized";
        Console.WriteLine($"Data1 InitializeAsync called - Value: {Value}");
        return Task.CompletedTask;
    }
}

public class Data2 : IAsyncInitializer
{
    [ClassDataSource<Data1>]
    public required Data1 Data1 { get; init; }

    public string Value { get; set; } = string.Empty;

    public virtual Task InitializeAsync()
    {
        // This should be called after Data1 has been injected and initialized
        Value = $"Data2 Initialized (Data1: {Data1?.Value ?? "NULL"})";
        Console.WriteLine($"Data2 InitializeAsync called - Value: {Value}, Data1: {Data1?.Value ?? "NULL"}");
        return Task.CompletedTask;
    }
}

// Data3 inherits from Data2, so it should inherit the Data1 property with its ClassDataSource attribute
public class Data3 : Data2
{
    public override Task InitializeAsync()
    {
        // This should be called after Data1 has been injected and initialized
        Value = $"Data3 Initialized (Data1: {Data1?.Value ?? "NULL"})";
        Console.WriteLine($"Data3 InitializeAsync called - Value: {Value}, Data1: {Data1?.Value ?? "NULL"}");
        return Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class InheritedDataSourceTests
{
    [ClassDataSource<Data3>(Shared = SharedType.PerTestSession)]
    public required Data3 Data3 { get; init; }

    [Test]
    public async Task Test_InheritedPropertyWithDataSource_ShouldBeInjected()
    {
        // The bug is that Data1 property (inherited from Data2) is not being injected
        // when Data3 is used as a ClassDataSource
        
        Console.WriteLine($"Test - Data3.Value: {Data3.Value}");
        Console.WriteLine($"Test - Data3.Data1: {Data3.Data1}");
        Console.WriteLine($"Test - Data3.Data1?.Value: {Data3.Data1?.Value}");
        
        // This assertion should pass but currently fails with the bug
        await Assert.That(Data3.Data1).IsNotNull();
        await Assert.That(Data3.Data1.Value).IsEqualTo("Data1 Initialized");
        await Assert.That(Data3.Value).Contains("Data1: Data1 Initialized");
    }
    
    [Test]
    [ClassDataSource<Data2>]
    public async Task Test_DirectDataSource_WorksCorrectly(Data2 data2)
    {
        // This test uses Data2 directly (not through inheritance) and should work
        // The framework should inject Data1 into Data2 directly
        
        // This should work because Data2's properties are defined directly on it
        await Assert.That(data2.Data1).IsNotNull();
        await Assert.That(data2.Data1.Value).IsEqualTo("Data1 Initialized");
        await Assert.That(data2.Value).Contains("Data1: Data1 Initialized");
    }
}

// Additional test case with multiple levels of inheritance
public class BaseDataWithSource
{
    [ClassDataSource<Data1>]
    public required Data1 BaseData1 { get; init; }
}

public class MiddleDataWithSource : BaseDataWithSource  
{
    [ClassDataSource<Data2>]
    public required Data2 MiddleData2 { get; init; }
}

public class DerivedDataWithSource : MiddleDataWithSource
{
    public string DerivedValue { get; set; } = "Derived";
}

[EngineTest(ExpectedResult.Pass)]
public class MultiLevelInheritanceTests
{
    [ClassDataSource<DerivedDataWithSource>]
    public required DerivedDataWithSource DerivedData { get; init; }
    
    [Test]
    public async Task Test_MultiLevelInheritance_AllDataSourcesShouldBeInjected()
    {
        // Both BaseData1 and MiddleData2 should be injected even though they're in base classes
        await Assert.That(DerivedData.BaseData1).IsNotNull();
        await Assert.That(DerivedData.BaseData1.Value).IsEqualTo("Data1 Initialized");
        
        await Assert.That(DerivedData.MiddleData2).IsNotNull();
        await Assert.That(DerivedData.MiddleData2.Data1).IsNotNull();
    }
}