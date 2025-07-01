using TUnit.Core;

namespace TUnit.UnitTests;

/// <summary>
/// Test class to verify AOT compatibility analyzer works correctly
/// These patterns should trigger analyzer warnings/errors
/// </summary>
public class AotCompatibilityAnalyzerTests
{
    [Test]
    public async Task ServiceProvider_IsAvailable()
    {
        // This should be fine - no AOT issues
        var context = TestContext.Current;
        await Assert.That(context).IsNotNull();
        await Assert.That(context!.ServiceProvider).IsNotNull();
    }

    // This generic test method should trigger TUnit0058 if not explicitly instantiated
    [Test]
    [GenerateGenericTest(typeof(string))]
    [GenerateGenericTest(typeof(object))]
    public async Task GenericTest_ShouldTriggerAnalyzer<T>()
        where T : class, new()
    {
        var instance = new T();
        await Assert.That(instance).IsNotNull();
    }

}

// This generic test class should trigger TUnit0058 if not explicitly instantiated
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(object))]
public class AotGenericTestClass<T>
    where T : class, new()
{
    [Test]
    public async Task GenericClassTest()
    {
        var instance = new T();
        await Assert.That(instance).IsNotNull();
    }
}

// This class shows the correct way - with explicit instantiation
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(object))]
public class AotCorrectGenericTestClass<T>
    where T : class, new()
{
    [Test]
    public async Task GenericClassTest()
    {
        var instance = new T();
        await Assert.That(instance).IsNotNull();
    }
}