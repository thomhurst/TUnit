using System.Text;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// StringBuilder specific assertions
[CreateAssertion(typeof(StringBuilder), typeof(StringBuilderAssertionExtensions), nameof(IsEmpty))]
[CreateAssertion(typeof(StringBuilder), typeof(StringBuilderAssertionExtensions), nameof(IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true)]

[CreateAssertion(typeof(StringBuilder), typeof(StringBuilderAssertionExtensions), nameof(IsAtCapacity))]
[CreateAssertion(typeof(StringBuilder), typeof(StringBuilderAssertionExtensions), nameof(IsAtCapacity), CustomName = "IsNotAtCapacity", NegateLogic = true)]

[CreateAssertion(typeof(StringBuilder), typeof(StringBuilderAssertionExtensions), nameof(HasExcessCapacity))]
[CreateAssertion(typeof(StringBuilder), typeof(StringBuilderAssertionExtensions), nameof(HasExcessCapacity), CustomName = "HasNoExcessCapacity", NegateLogic = true)]
public static partial class StringBuilderAssertionExtensions
{
    internal static bool IsEmpty(StringBuilder sb) => sb.Length == 0;
    internal static bool IsAtCapacity(StringBuilder sb) => sb.Length == sb.Capacity;
    internal static bool HasExcessCapacity(StringBuilder sb) => sb.Capacity > sb.Length;
}