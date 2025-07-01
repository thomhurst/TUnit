using System;
using TUnit.Core;

namespace TUnit.TestProject;

// Simple test to verify InheritsTests with generic base classes works
public abstract class GenericBase<T>
{
    [Test]
    public void BaseTest()
    {
        Console.WriteLine($"BaseTest with type: {typeof(T).Name}");
    }

    [Test]
    [Arguments("test")]
    [Arguments(42)]
    public void BaseTestWithArgs<TArg>(TArg arg)
    {
        Console.WriteLine($"BaseTestWithArgs: arg={arg}, argType={typeof(TArg).Name}, classType={typeof(T).Name}");
    }
}

[InheritsTests]
public class IntGenericVerification : GenericBase<int> { }

[InheritsTests]
public class StringGenericVerification : GenericBase<string> { }
