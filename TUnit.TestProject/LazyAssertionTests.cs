using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class LazyAssertionTests
{
    [Test]
    public async Task Test_Lazy_IsValueCreated()
    {
        var lazy = new Lazy<string>(() => "Hello");
        _ = lazy.Value; // Force value creation
        await Assert.That(lazy).IsValueCreated();
    }

    [Test]
    public async Task Test_Lazy_IsValueCreated_WithInt()
    {
        var lazy = new Lazy<int>(() => 42);
        _ = lazy.Value; // Force value creation
        await Assert.That(lazy).IsValueCreated();
    }

    [Test]
    public async Task Test_Lazy_IsValueNotCreated()
    {
        var lazy = new Lazy<string>(() => "Hello");
        await Assert.That(lazy).IsValueNotCreated();
    }

    [Test]
    public async Task Test_Lazy_IsValueNotCreated_WithObject()
    {
        var lazy = new Lazy<object>(() => new object());
        await Assert.That(lazy).IsValueNotCreated();
    }

    [Test]
    public async Task Test_Lazy_IsValueNotCreated_ThreadSafe()
    {
        var lazy = new Lazy<string>(() => "Hello", LazyThreadSafetyMode.ExecutionAndPublication);
        await Assert.That(lazy).IsValueNotCreated();
    }
}
