using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Exception type using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap exception property checks as extension methods.
/// </summary>
file static partial class ExceptionAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have an inner exception", InlineMethodBody = true)]
    public static bool HasInnerException(this Exception value) => value?.InnerException != null;
    [GenerateAssertion(ExpectationMessage = "to have no inner exception", InlineMethodBody = true)]
    public static bool HasNoInnerException(this Exception value) => value?.InnerException == null;
    [GenerateAssertion(ExpectationMessage = "to have a stack trace", InlineMethodBody = true)]
    public static bool HasStackTrace(this Exception value) => !string.IsNullOrWhiteSpace(value?.StackTrace);
    [GenerateAssertion(ExpectationMessage = "to have no data", InlineMethodBody = true)]
    public static bool HasNoData(this Exception value) => value?.Data.Count == 0;
    [GenerateAssertion(ExpectationMessage = "to have a help link", InlineMethodBody = true)]
    public static bool HasHelpLink(this Exception value) => !string.IsNullOrWhiteSpace(value?.HelpLink);
    [GenerateAssertion(ExpectationMessage = "to have no help link", InlineMethodBody = true)]
    public static bool HasNoHelpLink(this Exception value) => string.IsNullOrWhiteSpace(value?.HelpLink);
    [GenerateAssertion(ExpectationMessage = "to have a source", InlineMethodBody = true)]
    public static bool HasSource(this Exception value) => !string.IsNullOrWhiteSpace(value?.Source);
    [GenerateAssertion(ExpectationMessage = "to have no source", InlineMethodBody = true)]
    public static bool HasNoSource(this Exception value) => string.IsNullOrWhiteSpace(value?.Source);

    // TODO: These methods access TargetSite which causes IL2026 warnings when inlined,
    // and cannot be called as non-inlined methods due to generator limitations
    // [EditorBrowsable(EditorBrowsableState.Never)]
    // [GenerateAssertion(ExpectationMessage = "to have a target site")]
    // [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TargetSite is used for assertion purposes only, not for reflection-based operations")]
    // public static bool HasTargetSite(this Exception value) => value?.TargetSite != null;

    // [EditorBrowsable(EditorBrowsableState.Never)]
    // [GenerateAssertion(ExpectationMessage = "to have no target site")]
    // [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TargetSite is used for assertion purposes only, not for reflection-based operations")]
    // public static bool HasNoTargetSite(this Exception value) => value?.TargetSite == null;
}
