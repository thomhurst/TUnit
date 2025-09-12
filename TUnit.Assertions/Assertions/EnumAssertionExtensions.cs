using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions;

[CreateAssertion(typeof(Enum), nameof(Enum.IsDefined))]
[CreateAssertion(typeof(Enum), nameof(Enum.IsDefined), CustomName = "IsNotDefined", NegateLogic = true)]
[CreateAssertion(typeof(Enum), nameof(Enum.HasFlag))]
[CreateAssertion(typeof(Enum), nameof(Enum.HasFlag), CustomName = "DoesNotHaveFlag", NegateLogic = true)]
[CreateAssertion(typeof(Enum), nameof(Enum.TryParse), CustomName = "CanBeParsed")]
[CreateAssertion(typeof(Enum), nameof(Enum.TryParse), CustomName = "CannotBeParsed", NegateLogic = true)]
public class EnumAssertionExtensions;
