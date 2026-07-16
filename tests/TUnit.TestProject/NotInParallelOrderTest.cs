using System;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject;

[NotInParallel(Order = 2)]
public sealed class Test1
{
    [Test]
    public async Task TestCase1()
    {
        await Task.Delay(100);
        Console.WriteLine("Test 1 - Order 2");
    }
}

[NotInParallel(Order = 1)]
public sealed class Test2
{
    [Test]
    public Task TestCase2()
    {
        Console.WriteLine("Test 2 - Order 1");
        return Task.CompletedTask;
    }
}

[NotInParallel(Order = 3)]
public sealed class Test3
{
    [Test]
    public Task TestCase3()
    {
        Console.WriteLine("Test 3 - Order 3");
        return Task.CompletedTask;
    }
}