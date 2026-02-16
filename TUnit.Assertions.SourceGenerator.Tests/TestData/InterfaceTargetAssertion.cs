using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Assertion on an interface type should generate covariant extension method
/// allowing derived types to use the assertion without explicit casting.
/// </summary>
public interface ITestInterface
{
    string? Foo { get; set; }
}

public class TestImpl : ITestInterface
{
    public string? Foo { get; set; }
}

public static partial class InterfaceTargetAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have foo '{expected}'")]
    public static bool HasFoo(this ITestInterface target, string expected)
    {
        return target.Foo == expected;
    }

    [GenerateAssertion]
    public static bool IsNotNull(this ITestInterface target)
    {
        return target.Foo != null;
    }
}
