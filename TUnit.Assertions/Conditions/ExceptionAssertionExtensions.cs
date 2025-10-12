using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Exception type using [GenerateAssertion] attributes.
/// These wrap exception property checks as extension methods.
/// </summary>
public static partial class ExceptionAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have an inner exception")]
    public static bool HasInnerException(this Exception value) => value?.InnerException != null;

    [GenerateAssertion(ExpectationMessage = "to have no inner exception")]
    public static bool HasNoInnerException(this Exception value) => value?.InnerException == null;

    [GenerateAssertion(ExpectationMessage = "to have a stack trace")]
    public static bool HasStackTrace(this Exception value) => !string.IsNullOrWhiteSpace(value?.StackTrace);

    [GenerateAssertion(ExpectationMessage = "to have no data")]
    public static bool HasNoData(this Exception value) => value?.Data.Count == 0;

    [GenerateAssertion(ExpectationMessage = "to have a help link")]
    public static bool HasHelpLink(this Exception value) => !string.IsNullOrWhiteSpace(value?.HelpLink);

    [GenerateAssertion(ExpectationMessage = "to have no help link")]
    public static bool HasNoHelpLink(this Exception value) => string.IsNullOrWhiteSpace(value?.HelpLink);

    [GenerateAssertion(ExpectationMessage = "to have a source")]
    public static bool HasSource(this Exception value) => !string.IsNullOrWhiteSpace(value?.Source);

    [GenerateAssertion(ExpectationMessage = "to have no source")]
    public static bool HasNoSource(this Exception value) => string.IsNullOrWhiteSpace(value?.Source);

    [GenerateAssertion(ExpectationMessage = "to have a target site")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TargetSite is used for assertion purposes only, not for reflection-based operations")]
    public static bool HasTargetSite(this Exception value) => value?.TargetSite != null;

    [GenerateAssertion(ExpectationMessage = "to have no target site")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TargetSite is used for assertion purposes only, not for reflection-based operations")]
    public static bool HasNoTargetSite(this Exception value) => value?.TargetSite == null;
}
