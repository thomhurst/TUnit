using System.Collections.Generic;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class GenericTypeTests
{
    [Test]
    [Arguments(5)]
    [Arguments("hello")]
    [Arguments(3.14)]
    public async Task GenericMethod_WithSingleTypeParameter<T>(T value)
    {
        // Verify that the generic type was properly resolved
        await Assert.That(value!.GetType()).IsNotNull();
        await Assert.That(typeof(T).Name).IsNotEmpty();
    }
    
    [Test]
    [Arguments(1, "one")]
    [Arguments(2.5, "two and a half")]
    [Arguments(true, "yes")]
    public async Task GenericMethod_WithMultipleTypeParameters<T, U>(T first, U second)
    {
        await Assert.That(first!.GetType()).IsNotNull();
        await Assert.That(second!.GetType()).IsNotNull();
        await Assert.That(first.GetType()).IsNotEqualTo(second.GetType());
    }
    
    // Note: Arrays and collections in attributes are not supported in .NET Framework
    // These tests will work in .NET Core/5+ but are commented out for compatibility
    
    /*
    [Test]
    [Arguments(new int[] { 1, 2, 3 })]
    [Arguments(new string[] { "a", "b", "c" })]
    public void GenericMethod_WithArrayParameter<T>(T[] array)
    {
        Assert.That(array).IsNotNull();
        Assert.That(array.Length).IsGreaterThan(0);
    }
    
    [Test]
    [Arguments(new List<int> { 1, 2, 3 })]
    [Arguments(new List<string> { "x", "y", "z" })]
    public void GenericMethod_WithGenericCollectionParameter<T>(List<T> list)
    {
        Assert.That(list).IsNotNull();
        Assert.That(list.Count).IsGreaterThan(0);
    }
    */
    
    [Test]
    [Arguments("test")]
    public async Task GenericMethod_WithConstraint_Class<T>(T value) where T : class
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value.GetType().IsClass).IsTrue();
    }
    
    [Test]
    [Arguments(42)]
    [Arguments(true)]
    public async Task GenericMethod_WithConstraint_Struct<T>(T value) where T : struct
    {
        await Assert.That(value.GetType().IsValueType).IsTrue();
    }
}

// Test generic classes
public class GenericClassTests<T>
{
    private readonly T _defaultValue;
    
    public GenericClassTests()
    {
        _defaultValue = default(T)!;
    }
    
    [Test]
    [Arguments(5)]
    public async Task TestMethod_InGenericClass(T value)
    {
        await Assert.That(value!.GetType()).IsNotNull();
        if (_defaultValue != null)
        {
            await Assert.That(value).IsNotEqualTo(_defaultValue);
        }
    }
}

// Concrete instantiations for the generic class tests
public class IntGenericClassTests : GenericClassTests<int>
{
}

public class StringGenericClassTests : GenericClassTests<string>
{
}