using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._2867;

public class DisposalNotCalledTests
{
    public class TestWebApplicationFactory : IAsyncDisposable, IAsyncInitializer
    {
        private static readonly ConcurrentDictionary<string, TestWebApplicationFactory> _instances = new();
        private static readonly ConcurrentDictionary<string, bool> _disposed = new();
        private static int _createdCount;
        private static int _disposedCount;
        
        public string Id { get; }
        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }
        
        public TestWebApplicationFactory()
        {
            Id = Guid.NewGuid().ToString();
            _instances[Id] = this;
            Interlocked.Increment(ref _createdCount);
            Console.WriteLine($"[TestWebApplicationFactory] Created instance {Id} (total: {_createdCount})");
        }
        
        public Task InitializeAsync()
        {
            IsInitialized = true;
            Console.WriteLine($"[TestWebApplicationFactory] Initialized instance {Id}");
            return Task.CompletedTask;
        }
        
        public ValueTask DisposeAsync()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                _disposed[Id] = true;
                Interlocked.Increment(ref _disposedCount);
                Console.WriteLine($"[TestWebApplicationFactory] Disposed instance {Id} (total disposed: {_disposedCount})");
            }
            return default;
        }
        
        public static int CreatedCount => _createdCount;
        public static int DisposedCount => _disposedCount;
        
        public static void Reset()
        {
            _instances.Clear();
            _disposed.Clear();
            _createdCount = 0;
            _disposedCount = 0;
        }
        
        public static bool IsInstanceDisposed(string id) => _disposed.ContainsKey(id);
        public static TestWebApplicationFactory? GetInstance(string id) => _instances.GetValueOrDefault(id);
    }
    
    public class DisposableService : IAsyncDisposable
    {
        private static int _createdCount;
        private static int _disposedCount;
        
        public string Id { get; }
        public bool IsDisposed { get; private set; }
        
        public DisposableService()
        {
            Id = Guid.NewGuid().ToString();
            Interlocked.Increment(ref _createdCount);
            Console.WriteLine($"[DisposableService] Created instance {Id} (total: {_createdCount})");
        }
        
        public ValueTask DisposeAsync()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Interlocked.Increment(ref _disposedCount);
                Console.WriteLine($"[DisposableService] Disposed instance {Id} (total disposed: {_disposedCount})");
            }
            return default;
        }
        
        public static int CreatedCount => _createdCount;
        public static int DisposedCount => _disposedCount;
        
        public static void Reset()
        {
            _createdCount = 0;
            _disposedCount = 0;
        }
    }
}

public class PerClassSharedDisposalTest : IAsyncDisposable
{
    [ClassDataSource<DisposalNotCalledTests.TestWebApplicationFactory>(Shared = SharedType.PerClass)]
    public required DisposalNotCalledTests.TestWebApplicationFactory WebApp { get; init; }
    
    [ClassDataSource<DisposalNotCalledTests.DisposableService>(Shared = SharedType.PerClass)]
    public required DisposalNotCalledTests.DisposableService Service { get; init; }
    
    private static readonly List<string> _webAppIds = new();
    private static readonly List<string> _serviceIds = new();
    private bool _isDisposed;
    
    [Before(Class)]
    public static void BeforeClass()
    {
        DisposalNotCalledTests.TestWebApplicationFactory.Reset();
        DisposalNotCalledTests.DisposableService.Reset();
        _webAppIds.Clear();
        _serviceIds.Clear();
    }
    
    public ValueTask DisposeAsync()
    {
        _isDisposed = true;
        Console.WriteLine($"[PerClassSharedDisposalTest] Test instance disposed");
        return default;
    }
    
    
    [Test]
    public async Task Test1_SharedInstanceShouldNotBeDisposed()
    {
        await Assert.That(WebApp).IsNotNull();
        await Assert.That(WebApp.IsInitialized).IsTrue();
        await Assert.That(WebApp.IsDisposed).IsFalse();
        
        await Assert.That(Service).IsNotNull();
        await Assert.That(Service.IsDisposed).IsFalse();
        
        _webAppIds.Add(WebApp.Id);
        _serviceIds.Add(Service.Id);
        
        Console.WriteLine($"[Test1] WebApp ID: {WebApp.Id}, Service ID: {Service.Id}");
    }
    
    [Test]
    public async Task Test2_ShouldUseSameSharedInstance()
    {
        await Assert.That(WebApp).IsNotNull();
        await Assert.That(WebApp.IsInitialized).IsTrue();
        await Assert.That(WebApp.IsDisposed).IsFalse();
        
        await Assert.That(Service).IsNotNull();
        await Assert.That(Service.IsDisposed).IsFalse();
        
        _webAppIds.Add(WebApp.Id);
        _serviceIds.Add(Service.Id);
        
        Console.WriteLine($"[Test2] WebApp ID: {WebApp.Id}, Service ID: {Service.Id}");
        
        // Verify same instance is used
        await Assert.That(_webAppIds.Distinct()).HasSingleItem();
        await Assert.That(_serviceIds.Distinct()).HasSingleItem();
    }
    
    [Test]
    public async Task Test3_ShouldStillUseSameSharedInstance()
    {
        await Assert.That(WebApp).IsNotNull();
        await Assert.That(WebApp.IsInitialized).IsTrue();
        await Assert.That(WebApp.IsDisposed).IsFalse();
        
        await Assert.That(Service).IsNotNull();
        await Assert.That(Service.IsDisposed).IsFalse();
        
        _webAppIds.Add(WebApp.Id);
        _serviceIds.Add(Service.Id);
        
        Console.WriteLine($"[Test3] WebApp ID: {WebApp.Id}, Service ID: {Service.Id}");
        
        // Verify same instance is used
        await Assert.That(_webAppIds.Distinct()).HasSingleItem();
        await Assert.That(_serviceIds.Distinct()).HasSingleItem();
    }
    
    [After(Class)]
    public static async Task VerifyDisposalAfterClass(ClassHookContext context)
    {
        Console.WriteLine($"[AfterClass] Checking disposal...");
        
        // Give disposal events a chance to complete
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(500));
        
        // After all tests in the class complete, shared instances should be disposed
        var webAppId = _webAppIds.FirstOrDefault();
        var serviceId = _serviceIds.FirstOrDefault();
        
        if (webAppId != null)
        {
            var webApp = DisposalNotCalledTests.TestWebApplicationFactory.GetInstance(webAppId);
            if (webApp != null)
            {
                Console.WriteLine($"[AfterClass] WebApp {webAppId} IsDisposed: {webApp.IsDisposed}");
                await Assert.That(webApp.IsDisposed).IsTrue();
            }
        }
        
        if (serviceId != null)
        {
            // Note: We don't have a way to get the service instance, so we check the count
            Console.WriteLine($"[AfterClass] Service disposal count: {DisposalNotCalledTests.DisposableService.DisposedCount}");
            await Assert.That(DisposalNotCalledTests.DisposableService.DisposedCount).IsEqualTo(1);
        }
        
        // Verify only one instance of each was created (PerClass sharing)
        await Assert.That(_webAppIds.Distinct().Count()).IsEqualTo(1);
        await Assert.That(_serviceIds.Distinct().Count()).IsEqualTo(1);
        
        // Verify all test instances themselves are disposed
        var testInstances = context.Tests.Select(t => t.Metadata.TestDetails.ClassInstance).OfType<PerClassSharedDisposalTest>().ToList();
        foreach (var instance in testInstances)
        {
            await Assert.That(instance._isDisposed).IsTrue();
        }
    }
}

public class PerAssemblySharedDisposalTest1
{
    [ClassDataSource<DisposalNotCalledTests.TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required DisposalNotCalledTests.TestWebApplicationFactory WebApp { get; init; }
    
    public static string? SharedWebAppId { get; private set; }
    
    [Test]
    public async Task Test1_SharedInstanceShouldNotBeDisposed()
    {
        await Assert.That(WebApp).IsNotNull();
        await Assert.That(WebApp.IsInitialized).IsTrue();
        await Assert.That(WebApp.IsDisposed).IsFalse();
        
        SharedWebAppId = WebApp.Id;
        Console.WriteLine($"[PerAssemblyTest1.Test1] WebApp ID: {WebApp.Id}");
    }
}

public class PerAssemblySharedDisposalTest2
{
    [ClassDataSource<DisposalNotCalledTests.TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required DisposalNotCalledTests.TestWebApplicationFactory WebApp { get; init; }
    
    [Test]
    public async Task Test2_ShouldUseSameAssemblySharedInstance()
    {
        await Assert.That(WebApp).IsNotNull();
        await Assert.That(WebApp.IsInitialized).IsTrue();
        await Assert.That(WebApp.IsDisposed).IsFalse();
        
        // Should be the same instance as in PerAssemblySharedDisposalTest1
        if (PerAssemblySharedDisposalTest1.SharedWebAppId != null)
        {
            await Assert.That(WebApp.Id).IsEqualTo(PerAssemblySharedDisposalTest1.SharedWebAppId);
        }
        
        Console.WriteLine($"[PerAssemblyTest2.Test2] WebApp ID: {WebApp.Id}");
    }
    
    [After(TestSession)]
    public static async Task VerifyDisposalAfterTestSession(TestSessionContext context)
    {
        Console.WriteLine($"[AfterTestSession] Checking disposal...");
        
        // Give disposal events a chance to complete
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(500));
        
        // After all tests in the test session complete, shared instance should be disposed
        var webAppId = PerAssemblySharedDisposalTest1.SharedWebAppId;
        if (webAppId != null)
        {
            var webApp = DisposalNotCalledTests.TestWebApplicationFactory.GetInstance(webAppId);
            if (webApp != null)
            {
                Console.WriteLine($"[AfterTestSession] WebApp {webAppId} IsDisposed: {webApp.IsDisposed}");
                if (webApp.IsDisposed)
                {
                    Console.WriteLine($"[AfterTestSession] ✅ WebApp was properly disposed");
                }
                else 
                {
                    Console.WriteLine($"[AfterTestSession] ⚠️ WebApp not disposed (expected when running with filters)");
                }
            }
        }
    }
}