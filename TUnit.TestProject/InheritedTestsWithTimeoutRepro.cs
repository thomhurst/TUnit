namespace TUnit.TestProject;

// Base class with test methods that have timeout
public abstract class BaseTestWithTimeout
{
    [Test]
    [Timeout(5000)] // 5 second timeout
    public async Task TestWithTimeout()
    {
        await Console.Out.WriteLineAsync("Running base test with timeout");
        await Task.Delay(1000); // Should complete successfully
    }

    [Test]
    [Timeout(2000)] // 2 second timeout
    public async Task TestThatShouldTimeout()
    {
        await Console.Out.WriteLineAsync("Running test that should timeout");
        await Task.Delay(10000); // Should timeout
    }
}

// Derived class A with InheritsTests
[InheritsTests]
public sealed class DerivedTestA : BaseTestWithTimeout
{
}

// Derived class B with InheritsTests
[InheritsTests]
public sealed class DerivedTestB : BaseTestWithTimeout
{
}
