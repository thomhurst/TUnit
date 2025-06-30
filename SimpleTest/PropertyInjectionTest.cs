using System;
using System.Threading.Tasks;
using TUnit.Core;

namespace SimpleTest;

public interface ITestService
{
    string GetValue();
}

public class TestService : ITestService, IAsyncInitializable, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }
    
    public async Task InitializeAsync()
    {
        await Task.Delay(10);
        IsInitialized = true;
    }
    
    public string GetValue()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Service not initialized");
            
        return "Test Value";
    }
    
    public async ValueTask DisposeAsync()
    {
        await Task.Delay(10);
        IsDisposed = true;
    }
}

public class PropertyInjectionTests
{
    // Property that should be injected
    [Inject]
    public TestService? TestService { get; set; }
    
    // Another injectable property
    [Inject]
    public ITestService? TestServiceInterface { get; set; }
    
    [Test]
    public async Task PropertyInjection_ShouldInitializeService()
    {
        // Service should be injected and initialized
        if (TestService == null)
            throw new Exception("TestService was not injected");
            
        if (!TestService.IsInitialized)
            throw new Exception("TestService was not initialized");
            
        var value = TestService.GetValue();
        if (value != "Test Value")
            throw new Exception($"Expected 'Test Value' but got '{value}'");
            
        await Task.CompletedTask;
    }
    
    [Test]
    public void PropertyInjection_InterfaceInjection()
    {
        if (TestServiceInterface == null)
            throw new Exception("TestServiceInterface was not injected");
            
        var value = TestServiceInterface.GetValue();
        if (string.IsNullOrEmpty(value))
            throw new Exception("Interface injection failed");
    }
}

// Marker attribute for injection
[AttributeUsage(AttributeTargets.Property)]
public class InjectAttribute : Attribute
{
}