#pragma warning disable CA2012

using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;


public class AsyncTaskTests
{
    [Test]
    public async Task Func_Task_Is_Callable()
    {
        await TUnitAssert.That(() => Task.FromResult("Hello")).IsNotNullOrEmpty().And.IsEqualTo("Hello");
    }
    
    [Test]
    public async Task Func_Awaited_Task_Is_Callable()
    {
        await TUnitAssert.That(async () => await Task.FromResult("Hello")).IsNotNullOrEmpty().And.IsEqualTo("Hello");
    }
    
    [Test]
    public async Task Func_Awaited_ValueTask_Is_Callable()
    {
        await TUnitAssert.That(async () => await ValueTask.FromResult("Hello")).IsNotNullOrEmpty().And.IsEqualTo("Hello");
    }
    
    [Test]
    public async Task ValueTask_Is_Callable()
    {
        await TUnitAssert.That(ValueTask.FromResult("Hello")).IsNotNullOrEmpty().And.IsEqualTo("Hello");
    }
    
    [Test]
    public async Task Task_Is_Callable()
    {
        await TUnitAssert.That(Task.FromResult("Hello")).IsNotNullOrEmpty().And.IsEqualTo("Hello");
    }
    
    [Test]
    public async Task Func_Throws_Task_Is_Callable()
    {
        await TUnitAssert.ThrowsAsync(() => Task.FromException(new DivideByZeroException()));
    }
    
    [Test]
    public async Task Func_Throws_Awaited_Task_Is_Callable()
    {
        await TUnitAssert.ThrowsAsync(async () => await Task.FromException(new DivideByZeroException()));
    }
    
    [Test]
    public async Task Func_Throws_Awaited_ValueTask_Is_Callable()
    {
        await TUnitAssert.ThrowsAsync(async () => await ValueTask.FromException(new DivideByZeroException()));
    }
    
    [Test]
    public async Task Throws_Task_Is_Callable()
    {
        await TUnitAssert.ThrowsAsync(Task.FromException(new DivideByZeroException()));
    }
    
    [Test]
    public async Task Throws_ValueTask_Is_Callable()
    {
        await TUnitAssert.ThrowsAsync(ValueTask.FromException(new DivideByZeroException()));
    }
}