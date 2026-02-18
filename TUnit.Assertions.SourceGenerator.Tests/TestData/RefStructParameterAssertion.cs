#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Method with ref struct parameter (DefaultInterpolatedStringHandler)
/// The generator should convert the ref struct to string before storing it
/// </summary>
public static class RefStructParameterAssertions
{
    /// <summary>
    /// Test that interpolated string handlers are properly converted to strings
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to contain {message}", InlineMethodBody = true)]
    public static bool ContainsMessage(this string value, ref DefaultInterpolatedStringHandler message)
        => value.Contains(message.ToStringAndClear());

    /// <summary>
    /// Test with a simpler expression body
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to end with {suffix}", InlineMethodBody = true)]
    public static bool EndsWithMessage(this string value, ref DefaultInterpolatedStringHandler suffix)
        => value.EndsWith(suffix.ToStringAndClear());
}
#endif
