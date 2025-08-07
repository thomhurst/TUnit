using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2632;

/// <summary>
/// Test to verify that disposal order respects scope hierarchy.
/// This reproduces the issue from #2632 where PerAssembly objects were disposing 
/// before PerClass objects finished, causing ObjectDisposedException.
/// </summary>

[EngineTest(ExpectedResult.Pass)]
public class DisposalOrderTestClass1
{
    [ClassDataSource<ConnectionPool>(Shared = SharedType.PerAssembly)]
    public required ConnectionPool ConnectionPool { get; init; }
    
    [ClassDataSource<WebAppFactory>(Shared = SharedType.PerClass)]
    public required WebAppFactory WebAppFactory { get; init; }

    [Test]
    public async Task Test1()
    {
        // This should work - ConnectionPool should not be disposed yet
        await ConnectionPool.DoSomething();
        await WebAppFactory.DoSomething();
    }

    [Test] 
    public async Task Test2()
    {
        // This should work - ConnectionPool should not be disposed yet
        await ConnectionPool.DoSomething();
        await WebAppFactory.DoSomething();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class DisposalOrderTestClass2
{
    [ClassDataSource<ConnectionPool>(Shared = SharedType.PerAssembly)]
    public required ConnectionPool ConnectionPool { get; init; }
    
    [ClassDataSource<WebAppFactory>(Shared = SharedType.PerClass)]  
    public required WebAppFactory WebAppFactory { get; init; }

    [Test]
    public async Task Test3()
    {
        // The key test: ConnectionPool should still be available for the second class
        // Before the fix, ConnectionPool would be disposed after the first class completed
        await ConnectionPool.DoSomething();
        await WebAppFactory.DoSomething();
    }
}

/// <summary>
/// Mock ConnectionPool that simulates a shared resource (e.g., database connection pool)
/// </summary>
public class ConnectionPool : IAsyncDisposable
{
    private static int _instanceCount = 0;
    private readonly int _instanceId;
    
    public ConnectionPool()
    {
        _instanceId = ++_instanceCount;
    }
    
    public bool Disposed { get; private set; }

    public ValueTask DoSomething()
    {
        if (Disposed)
        {
            throw new ObjectDisposedException($"ConnectionPool-{_instanceId} was already disposed!");
        }
        return default;
    }

    public ValueTask DisposeAsync()
    {
        if (!Disposed)
        {
            Disposed = true;
        }
        return default;
    }
}

/// <summary>
/// Mock WebAppFactory that depends on ConnectionPool
/// This simulates the real-world scenario where PerClass objects depend on PerAssembly objects
/// </summary>
public class WebAppFactory : IAsyncDisposable
{
    private static int _instanceCount = 0;
    private readonly int _instanceId;
    
    public WebAppFactory()
    {
        _instanceId = ++_instanceCount;
    }
    
    public bool Disposed { get; private set; }

    public ValueTask DoSomething()
    {
        if (Disposed)
        {
            throw new ObjectDisposedException($"WebAppFactory-{_instanceId} was already disposed!");
        }
        return default;
    }

    public ValueTask DisposeAsync()
    {
        if (!Disposed)
        {
            Disposed = true;
        }
        return default;
    }
}