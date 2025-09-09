using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.InheritanceSharedTypeRepro;

// Simulating the user's container wrappers
public class PostgreSqlContainerWrapper : IDisposable, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }
    public string InstanceId { get; } = Guid.NewGuid().ToString();

    public void Dispose()
    {
        IsDisposed = true;
        Console.WriteLine($"PostgreSQL Container {InstanceId} disposed (sync)");
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        Console.WriteLine($"PostgreSQL Container {InstanceId} disposed (async)");
        return default;
    }
}

public class RabbitMqContainerWrapper : IDisposable, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }
    public string InstanceId { get; } = Guid.NewGuid().ToString();

    public void Dispose()
    {
        IsDisposed = true;
        Console.WriteLine($"RabbitMQ Container {InstanceId} disposed (sync)");
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        Console.WriteLine($"RabbitMQ Container {InstanceId} disposed (async)");
        return default;
    }
}

// Base application testing server (like in the user's example)
public class BaseApplicationTestingServer<T>
{
    // Base functionality
}

// The working scenario - containers defined directly on TestingServer
public class TestingServerWorking : BaseApplicationTestingServer<object>
{
    [ClassDataSource<PostgreSqlContainerWrapper>(Shared = SharedType.PerTestSession)]
    public required PostgreSqlContainerWrapper PostgresContainerWrapper { get; set; } = new();

    [ClassDataSource<RabbitMqContainerWrapper>(Shared = SharedType.PerTestSession)]
    public required RabbitMqContainerWrapper RabbitContainerWrapper { get; set; } = new();
}

// The broken scenario - containers defined on intermediate base class
public abstract class ModuleTestingServer : BaseApplicationTestingServer<object>
{
    [ClassDataSource<PostgreSqlContainerWrapper>(Shared = SharedType.PerTestSession)]
    public required PostgreSqlContainerWrapper PostgresContainerWrapper { get; set; } = new();

    [ClassDataSource<RabbitMqContainerWrapper>(Shared = SharedType.PerTestSession)]
    public required RabbitMqContainerWrapper RabbitContainerWrapper { get; set; } = new();
}

public class TestingServerBroken : ModuleTestingServer
{
    // Inherits the ClassDataSource properties from ModuleTestingServer
}

// Test classes for the working scenario
[EngineTest(ExpectedResult.Pass)]
public class WorkingTestClass1
{
    [ClassDataSource<TestingServerWorking>(Shared = SharedType.PerClass)]
    public required TestingServerWorking TestingServer { get; set; } = default!;

    [Test, NotInParallel(Order = 1)]
    public async Task Test1()
    {
        Console.WriteLine($"WorkingTestClass1.Test1 - PostgreSQL: {TestingServer.PostgresContainerWrapper.InstanceId}, RabbitMQ: {TestingServer.RabbitContainerWrapper.InstanceId}");
        
        await Assert.That(TestingServer.PostgresContainerWrapper.IsDisposed).IsFalse();
        await Assert.That(TestingServer.RabbitContainerWrapper.IsDisposed).IsFalse();
    }

    [Test, NotInParallel(Order = 2)]
    public async Task Test2()
    {
        Console.WriteLine($"WorkingTestClass1.Test2 - PostgreSQL: {TestingServer.PostgresContainerWrapper.InstanceId}, RabbitMQ: {TestingServer.RabbitContainerWrapper.InstanceId}");
        
        await Assert.That(TestingServer.PostgresContainerWrapper.IsDisposed).IsFalse();
        await Assert.That(TestingServer.RabbitContainerWrapper.IsDisposed).IsFalse();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class WorkingTestClass2
{
    [ClassDataSource<TestingServerWorking>(Shared = SharedType.PerClass)]
    public required TestingServerWorking TestingServer { get; set; } = default!;

    [Test, NotInParallel(Order = 3)]
    public async Task Test3()
    {
        Console.WriteLine($"WorkingTestClass2.Test3 - PostgreSQL: {TestingServer.PostgresContainerWrapper.InstanceId}, RabbitMQ: {TestingServer.RabbitContainerWrapper.InstanceId}");
        
        await Assert.That(TestingServer.PostgresContainerWrapper.IsDisposed).IsFalse();
        await Assert.That(TestingServer.RabbitContainerWrapper.IsDisposed).IsFalse();
    }

    [Test, NotInParallel(Order = 4)]
    public async Task Test4()
    {
        Console.WriteLine($"WorkingTestClass2.Test4 - PostgreSQL: {TestingServer.PostgresContainerWrapper.InstanceId}, RabbitMQ: {TestingServer.RabbitContainerWrapper.InstanceId}");
        
        await Assert.That(TestingServer.PostgresContainerWrapper.IsDisposed).IsFalse();
        await Assert.That(TestingServer.RabbitContainerWrapper.IsDisposed).IsFalse();
    }
}

// Test classes for the broken scenario
[EngineTest(ExpectedResult.Pass)]
public class BrokenTestClass1
{
    [ClassDataSource<TestingServerBroken>(Shared = SharedType.PerClass)]
    public required TestingServerBroken TestingServer { get; set; } = default!;

    [Test, NotInParallel(Order = 5)]
    public async Task Test5()
    {
        Console.WriteLine($"BrokenTestClass1.Test5 - PostgreSQL: {TestingServer.PostgresContainerWrapper.InstanceId}, RabbitMQ: {TestingServer.RabbitContainerWrapper.InstanceId}");
        
        await Assert.That(TestingServer.PostgresContainerWrapper.IsDisposed).IsFalse();
        await Assert.That(TestingServer.RabbitContainerWrapper.IsDisposed).IsFalse();
    }

    [Test, NotInParallel(Order = 6)]
    public async Task Test6()
    {
        Console.WriteLine($"BrokenTestClass1.Test6 - PostgreSQL: {TestingServer.PostgresContainerWrapper.InstanceId}, RabbitMQ: {TestingServer.RabbitContainerWrapper.InstanceId}");
        
        await Assert.That(TestingServer.PostgresContainerWrapper.IsDisposed).IsFalse();
        await Assert.That(TestingServer.RabbitContainerWrapper.IsDisposed).IsFalse();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class BrokenTestClass2
{
    [ClassDataSource<TestingServerBroken>(Shared = SharedType.PerClass)]
    public required TestingServerBroken TestingServer { get; set; } = default!;

    [Test, NotInParallel(Order = 7)]
    public async Task Test7()
    {
        Console.WriteLine($"BrokenTestClass2.Test7 - PostgreSQL: {TestingServer.PostgresContainerWrapper.InstanceId}, RabbitMQ: {TestingServer.RabbitContainerWrapper.InstanceId}");
        
        await Assert.That(TestingServer.PostgresContainerWrapper.IsDisposed).IsFalse();
        await Assert.That(TestingServer.RabbitContainerWrapper.IsDisposed).IsFalse();
    }

    [Test, NotInParallel(Order = 8)]
    public async Task Test8()
    {
        Console.WriteLine($"BrokenTestClass2.Test8 - PostgreSQL: {TestingServer.PostgresContainerWrapper.InstanceId}, RabbitMQ: {TestingServer.RabbitContainerWrapper.InstanceId}");
        
        await Assert.That(TestingServer.PostgresContainerWrapper.IsDisposed).IsFalse();
        await Assert.That(TestingServer.RabbitContainerWrapper.IsDisposed).IsFalse();
    }
}