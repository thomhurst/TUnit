using TUnit.Core;
using TUnit.Assertions;
using System.Collections.Generic;

namespace TUnit.TestProject;

/// <summary>
/// Simple tests to verify generic type support
/// </summary>
public class SimpleGenericMethodTests
{
    [Test]
    [Arguments(42)]
    [Arguments("hello")]
    public async Task GenericMethod_SimpleCase<T>(T value)
    {
        await Assert.That(value).IsNotNull();
    }
}

/// <summary>
/// Simple generic class test
/// </summary>
public class SimpleGenericClassTests<T>
{
    [Test]
    [Arguments(42)]  // Will create SimpleGenericClassTests<int>
    public async Task TestWithValue(T value)
    {
        await Assert.That(value).IsNotNull();
    }
}