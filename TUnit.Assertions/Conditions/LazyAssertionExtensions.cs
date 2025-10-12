using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Lazy&lt;T&gt; type using [GenerateAssertion] attributes.
/// These wrap lazy initialization checks as extension methods.
/// </summary>
public static partial class LazyAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have its value created")]
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Only checking IsValueCreated property, not creating instances")]
    public static bool IsValueCreated<T>(this Lazy<T> value) => value?.IsValueCreated == true;

    [GenerateAssertion(ExpectationMessage = "to not have its value created")]
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Only checking IsValueCreated property, not creating instances")]
    public static bool IsValueNotCreated<T>(this Lazy<T> value) => value?.IsValueCreated == false;
}
