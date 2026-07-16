using System.Threading;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: <see cref="GenerateAssertionAttribute"/>-decorated method whose parameter
/// is a non-nullable value type declared with <c>= default</c>. The Roslyn-reported default
/// expression is <see langword="null"/>, but emitting <c>parameter = null</c> for a value
/// type is invalid C# (CS1750). The generator must render the bare <c>default</c> literal,
/// which the C# compiler infers as <c>default(TypeName)</c> from the parameter type.
/// </summary>
public static partial class ValueTypeDefaultParameterAssertionExtensions
{
    [GenerateAssertion]
    public static bool RespectsToken(this int value, CancellationToken token = default)
    {
        return !token.IsCancellationRequested && value > 0;
    }
}
