using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for string type using [AssertionFrom&lt;string&gt;] attributes.
/// These wrap static methods from the string class.
/// </summary>
[AssertionFrom<string>("IsNullOrWhiteSpace", ExpectationMessage = "be null, empty, or whitespace")]
[AssertionFrom<string>("IsNullOrEmpty", ExpectationMessage = "be null or empty")]
[AssertionFrom<string>("IsNullOrEmpty", CustomName = "IsNotNullOrEmpty", NegateLogic = true, ExpectationMessage = "be null or empty")]
public static partial class StringStaticMethodAssertions
{
}
