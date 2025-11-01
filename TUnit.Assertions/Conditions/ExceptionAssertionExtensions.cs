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

    [GenerateAssertion(ExpectationMessage = "to have a target site", InlineMethodBody = true)]
    [RequiresUnreferencedCode("Exception.TargetSite uses reflection which may be trimmed in AOT scenarios")]
    public static bool HasTargetSite(this Exception value) => value?.TargetSite != null;

    [GenerateAssertion(ExpectationMessage = "to have no target site", InlineMethodBody = true)]
    [RequiresUnreferencedCode("Exception.TargetSite uses reflection which may be trimmed in AOT scenarios")]
    public static bool HasNoTargetSite(this Exception value) => value?.TargetSite == null;
}
