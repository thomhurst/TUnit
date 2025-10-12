using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Exception type using [GenerateAssertion] attributes.
/// These wrap exception property checks as extension methods.
/// </summary>
public static class ExceptionAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have an inner exception")]
    public static bool HasInnerException(this Exception value) => value?.InnerException != null;

    [GenerateAssertion(ExpectationMessage = "to have no inner exception")]
    public static bool HasNoInnerException(this Exception value) => value?.InnerException == null;

    [GenerateAssertion(ExpectationMessage = "to have a stack trace")]
    public static bool HasStackTrace(this Exception value) => !string.IsNullOrWhiteSpace(value?.StackTrace);

    [GenerateAssertion(ExpectationMessage = "to have no data")]
    public static bool HasNoData(this Exception value) => value?.Data.Count == 0;
}
