using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.TestProject;

public class NullableDelegateTests
{
    [Test]
    public async Task TestNullableFunc()
    {
        Func<int>? nullableFunc = null;
        
        // Test that null func can be checked
        await Assert.That<Func<int>?>(nullableFunc).IsNull();
        
        nullableFunc = () => 42;
        
        // Test that non-null func can be asserted
        var nonNullFunc = await Assert.That<Func<int>?>(nullableFunc).IsNotNull();
        
        // After asserting not null, we should be able to call it without warnings
        var result = nonNullFunc();
        await Assert.That(result).IsEqualTo(42);
    }
    
    [Test]
    public async Task TestNullableAction()
    {
        Action? nullableAction = null;
        
        await Assert.That<Action?>(nullableAction).IsNull();
        
        var wasExecuted = false;
        nullableAction = () => wasExecuted = true;
        
        var nonNullAction = await Assert.That<Action?>(nullableAction).IsNotNull();
        
        // Execute the action after asserting not null
        nonNullAction();
        await Assert.That(wasExecuted).IsTrue();
    }
    
    [Test]
    public async Task TestNullableAsyncFunc()
    {
        Func<Task<int>>? asyncFunc = null;
        
        await Assert.That<Func<Task<int>>?>(asyncFunc).IsNull();
        
        asyncFunc = async () => 
        {
            await Task.Delay(1);
            return 42;
        };
        
        var nonNullFunc = await Assert.That<Func<Task<int>>?>(asyncFunc).IsNotNull();
        var result = await nonNullFunc();
        await Assert.That(result).IsEqualTo(42);
    }
    
    [Test]
    public async Task TestNullableAsyncAction()
    {
        Func<Task>? asyncAction = null;
        
        await Assert.That<Func<Task>?>(asyncAction).IsNull();
        
        var wasExecuted = false;
        asyncAction = async () => 
        {
            await Task.Delay(1);
            wasExecuted = true;
        };
        
        var nonNullAction = await Assert.That<Func<Task>?>(asyncAction).IsNotNull();
        await nonNullAction();
        await Assert.That(wasExecuted).IsTrue();
    }
    
    [Test]
    public async Task TestRegularReference()
    {
        string? nullableString = null;
        
        await Assert.That(nullableString).IsNull();
        
        nullableString = "test";
        
        var nonNullString = await Assert.That(nullableString).IsNotNull();
        
        // After asserting not null, we can use it without warnings
        var length = nonNullString.Length;
        await Assert.That(length).IsEqualTo(4);
    }
}