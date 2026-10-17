using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7092;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7092.
/// Each class passes in source-generated mode and failed in reflection mode (or the reverse).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ParameterlessMethodDataSourceTests
{
    public static IEnumerable<object[]> SingleEmptyRow() => [[]];

    // Reflection mode threw "No members to generate were provided" for a parameterless test method.
    [Test]
    [MethodDataSource(nameof(SingleEmptyRow))]
    public async Task Parameterless_Test_With_Method_Data_Source_Runs()
    {
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ValueTaskMethodDataSourceTests
{
    public static async ValueTask<IEnumerable<int>> ValueTaskRows()
    {
        await Task.Yield();
        return [1, 2];
    }

    // Reflection mode never awaited the ValueTask and passed it to the test as one argument.
    [Test]
    [MethodDataSource(nameof(ValueTaskRows))]
    public async Task ValueTask_Data_Source_Is_Awaited(int value)
    {
        await Assert.That(value).IsBetween(1, 2);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestWithArgumentsTests
{
    // Source-gen mode pairs each row with the instantiation it fits: <int>(5) and <string>("hello").
    // Reflection mode also produced <int>("hello") and <string>(5).
    [Test]
    [Arguments(5)]
    [Arguments("hello")]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(string))]
    public async Task Each_Row_Runs_Only_For_Its_Own_Type<T>(T value)
    {
        await Assert.That(value!.GetType()).IsEqualTo(typeof(T));
    }
}

public abstract class GenericBaseWithClassHook<T>
{
    protected static readonly ConcurrentDictionary<Type, bool> HookRanFor = new();

    // Reflection mode invoked this on the open generic type and failed with
    // "Late bound operations cannot be performed on types or methods for which ContainsGenericParameters is true".
    [Before(Class)]
    public static Task RecordHook()
    {
        HookRanFor[typeof(T)] = true;
        return Task.CompletedTask;
    }

    [Test]
    public async Task Static_Class_Hook_On_Generic_Base_Runs_For_Closed_Type()
    {
        await Assert.That(HookRanFor.ContainsKey(typeof(T))).IsTrue();
    }
}

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class GenericBaseWithClassHookTests : GenericBaseWithClassHook<Uri>;
