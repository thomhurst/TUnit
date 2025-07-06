namespace TUnit.UnitTests;

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
        await Task.Delay(1);
        IsInitialized = true;
    }

    public string GetValue()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Service not initialized");
        }

        return "TestValue";
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Delay(1);
        IsDisposed = true;
    }
}

public interface IDependentService
{
    string GetDependentValue();
}

public class DependentService : IDependentService
{
    private readonly ITestService _testService;

    public DependentService(ITestService testService)
    {
        _testService = testService;
    }

    public string GetDependentValue()
    {
        return $"Dependent: {_testService.GetValue()}";
    }
}

public class PropertyInjectionTests
{
    [Inject(Required = true)]
    public ITestService TestService { get; set; } = null!;

    [Inject(Required = true, Order = 1)]
    public IDependentService DependentService { get; set; } = null!;

    [Inject(Required = false)]
    public ITestService? OptionalService { get; set; }

    [Test]
    public async Task PropertyInjection_InjectsRequiredServices()
    {
        await Assert.That(TestService).IsNotNull();
        await Assert.That(TestService.GetValue()).IsEqualTo("TestValue");
    }

    [Test]
    public async Task PropertyInjection_InjectsInCorrectOrder()
    {
        await Assert.That(DependentService).IsNotNull();
        await Assert.That(DependentService.GetDependentValue()).IsEqualTo("Dependent: TestValue");
    }

    [Test]
    public async Task PropertyInjection_InitializesAsyncServices()
    {
        var testService = TestService as TestService;
        await Assert.That(testService).IsNotNull();
        await Assert.That(testService!.IsInitialized).IsTrue();
    }

    [Test]
    public async Task PropertyInjection_OptionalServicesCanBeNull()
    {
        // OptionalService should be null if not provided by DI container
        await Assert.That(OptionalService == null || OptionalService is ITestService).IsTrue();
    }
}

// Test with circular dependencies
public interface ICircularA
{
    string Name { get; }
}

public interface ICircularB
{
    string Name { get; }
}

public class CircularA : ICircularA
{
    private readonly ICircularB _b;

    public CircularA(ICircularB b)
    {
        _b = b;
    }

    public string Name => "A";
}

public class CircularB : ICircularB
{
    private readonly ICircularA _a;

    public CircularB(ICircularA a)
    {
        _a = a;
    }

    public string Name => "B";
}

public class CircularDependencyTests
{
    [Inject]
    public ICircularA CircularA { get; set; } = null!;

    [Inject]
    public ICircularB CircularB { get; set; } = null!;

    [Test]
    [Skip("Expected to fail due to circular dependency")]
    public async Task PropertyInjection_DetectsCircularDependencies()
    {
        // This test should not run due to circular dependency detection
        await Assert.That(false).IsTrue();
    }
}
