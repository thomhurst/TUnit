using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.SourceGenerator.Tests.TestData;

/// <summary>
/// Test case: Simple method decorated with [GenerateAssertOverloads]
/// Should generate wrapper types and overloads for Func, Task, and ValueTask variants.
/// </summary>
public static partial class TestAssert
{
    [GenerateAssertOverloads(Priority = 3)]
    public static string That(string? value) => value ?? "";
}
