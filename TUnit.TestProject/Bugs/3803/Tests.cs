namespace TUnit.TestProject.Bugs._3803;

/// <summary>
/// Bug #3803: Nested dependencies with SharedType.PerTestSession are instantiated multiple times
///
/// Expected behavior:
/// - TestRabbitContainer should be instantiated ONCE per test session (InstanceCount == 1)
/// - TestSqlContainer should be instantiated ONCE per test session (InstanceCount == 1)
/// - All WebApplicationFactory instances should receive the SAME container instances
///
/// Actual behavior (BUG):
/// - Containers are instantiated multiple times (once per test or once per factory)
/// - Each WebApplicationFactory receives DIFFERENT container instances
/// </summary>

[NotInParallel]
public class Bug3803_TestClass1
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory Factory { get; init; }

    [Test]
    public async Task Test1_VerifyContainersAreShared()
    {
        Console.WriteLine($"[Bug3803_TestClass1.Test1] Factory Instance: #{Factory.InstanceId}");
        Console.WriteLine($"[Bug3803_TestClass1.Test1] RabbitContainer Instance: #{Factory.RabbitContainer.InstanceId}");
        Console.WriteLine($"[Bug3803_TestClass1.Test1] SqlContainer Instance: #{Factory.SqlContainer.InstanceId}");

        // Verify containers are initialized
        await Assert.That(Factory.RabbitContainer.IsInitialized).IsTrue();
        await Assert.That(Factory.SqlContainer.IsInitialized).IsTrue();

        // BUG VERIFICATION: These should ALWAYS be 1 if SharedType.PerTestSession works correctly
        await Assert.That(TestRabbitContainer.InstanceCount).IsEqualTo(1);
        await Assert.That(TestSqlContainer.InstanceCount).IsEqualTo(1);

        // All instances should have ID = 1 (first and only instance)
        await Assert.That(Factory.RabbitContainer.InstanceId).IsEqualTo(1);
        await Assert.That(Factory.SqlContainer.InstanceId).IsEqualTo(1);
    }

    [Test]
    public async Task Test2_VerifyContainersAreStillShared()
    {
        Console.WriteLine($"[Bug3803_TestClass1.Test2] Factory Instance: #{Factory.InstanceId}");
        Console.WriteLine($"[Bug3803_TestClass1.Test2] RabbitContainer Instance: #{Factory.RabbitContainer.InstanceId}");
        Console.WriteLine($"[Bug3803_TestClass1.Test2] SqlContainer Instance: #{Factory.SqlContainer.InstanceId}");

        // Same assertions as Test1 - containers should still be the same instances
        await Assert.That(TestRabbitContainer.InstanceCount).IsEqualTo(1);
        await Assert.That(TestSqlContainer.InstanceCount).IsEqualTo(1);
        await Assert.That(Factory.RabbitContainer.InstanceId).IsEqualTo(1);
        await Assert.That(Factory.SqlContainer.InstanceId).IsEqualTo(1);
    }

    [Test]
    public async Task Test3_VerifyInitializationCalledOnce()
    {
        Console.WriteLine($"[Bug3803_TestClass1.Test3] RabbitContainer InitializeCount: {TestRabbitContainer.InitializeCount}");
        Console.WriteLine($"[Bug3803_TestClass1.Test3] SqlContainer InitializeCount: {TestSqlContainer.InitializeCount}");

        // Initialize should be called only once per container
        await Assert.That(TestRabbitContainer.InitializeCount).IsEqualTo(1);
        await Assert.That(TestSqlContainer.InitializeCount).IsEqualTo(1);
    }
}

[NotInParallel]
public class Bug3803_TestClass2
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory Factory { get; init; }

    [Test]
    public async Task Test1_DifferentClassShouldGetSameContainers()
    {
        Console.WriteLine($"[Bug3803_TestClass2.Test1] Factory Instance: #{Factory.InstanceId}");
        Console.WriteLine($"[Bug3803_TestClass2.Test1] RabbitContainer Instance: #{Factory.RabbitContainer.InstanceId}");
        Console.WriteLine($"[Bug3803_TestClass2.Test1] SqlContainer Instance: #{Factory.SqlContainer.InstanceId}");

        // Even in a different test class, we should get the SAME container instances
        await Assert.That(TestRabbitContainer.InstanceCount).IsEqualTo(1);
        await Assert.That(TestSqlContainer.InstanceCount).IsEqualTo(1);

        // Should be the same instance (ID = 1)
        await Assert.That(Factory.RabbitContainer.InstanceId).IsEqualTo(1);
        await Assert.That(Factory.SqlContainer.InstanceId).IsEqualTo(1);
    }

    [Test]
    public async Task Test2_VerifyContainersAreInitialized()
    {
        await Assert.That(Factory.RabbitContainer.IsInitialized).IsTrue();
        await Assert.That(Factory.SqlContainer.IsInitialized).IsTrue();
    }
}

[NotInParallel]
public class Bug3803_TestClass3
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory Factory { get; init; }

    [Test]
    public async Task Test1_ThirdClassAlsoGetsSameContainers()
    {
        Console.WriteLine($"[Bug3803_TestClass3.Test1] Factory Instance: #{Factory.InstanceId}");
        Console.WriteLine($"[Bug3803_TestClass3.Test1] RabbitContainer Instance: #{Factory.RabbitContainer.InstanceId}");
        Console.WriteLine($"[Bug3803_TestClass3.Test1] SqlContainer Instance: #{Factory.SqlContainer.InstanceId}");

        // Still the same instances
        await Assert.That(TestRabbitContainer.InstanceCount).IsEqualTo(1);
        await Assert.That(TestSqlContainer.InstanceCount).IsEqualTo(1);
        await Assert.That(Factory.RabbitContainer.InstanceId).IsEqualTo(1);
        await Assert.That(Factory.SqlContainer.InstanceId).IsEqualTo(1);
    }

    [Test]
    public async Task Test2_FinalVerification()
    {
        Console.WriteLine($"[Bug3803_TestClass3.Test2] Final verification");
        Console.WriteLine($"  Total RabbitContainer instances: {TestRabbitContainer.InstanceCount}");
        Console.WriteLine($"  Total SqlContainer instances: {TestSqlContainer.InstanceCount}");
        Console.WriteLine($"  Total WebApplicationFactory instances: {WebApplicationFactory.InstanceCount}");

        // Final assertion: containers should have been instantiated exactly once
        await Assert.That(TestRabbitContainer.InstanceCount).IsEqualTo(1);
        await Assert.That(TestSqlContainer.InstanceCount).IsEqualTo(1);
    }
}
