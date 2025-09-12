using System;
using System.Collections.Generic;
using TUnit.Core;

namespace TUnit.TestProject;

[NotInParallel]
public sealed class HookParityTest
{
    private bool _hookExecuted = false;

    [Before(HookType.Test)]
    public void Setup()
    {
        _hookExecuted = true;
        Console.WriteLine("C# Hook executed successfully!");
    }

    [Test]
    [MethodDataSource(nameof(GetValues))]
    public void DoSomething(int value)
    {
        // Ensure the hook was executed
        if (!_hookExecuted)
        {
            throw new InvalidOperationException("C# hook was not executed!");
        }
        
        Console.WriteLine($"C# test executed with value: {value}, hook executed: {_hookExecuted}");
    }

    public static IEnumerable<int> GetValues()
    {
        return new int[] { 1, 2, 3, 4 };
    }
}