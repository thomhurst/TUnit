using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Version type using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap version number checks as extension methods.
/// </summary>
file static partial class VersionAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be a major version (x.0.0.0)", InlineMethodBody = true)]
    public static bool IsMajorVersion(this Version value) =>
        value != null && value.Minor == 0 && (value.Build <= 0 || value.Build == -1) && (value.Revision <= 0 || value.Revision == -1);
    [GenerateAssertion(ExpectationMessage = "to not be a major version", InlineMethodBody = true)]
    public static bool IsNotMajorVersion(this Version value) =>
        value != null && (value.Minor != 0 || (value.Build > 0 && value.Build != -1) || (value.Revision > 0 && value.Revision != -1));
    [GenerateAssertion(ExpectationMessage = "to have a build number", InlineMethodBody = true)]
    public static bool HasBuildNumber(this Version value) => value?.Build >= 0;
    [GenerateAssertion(ExpectationMessage = "to not have a build number", InlineMethodBody = true)]
    public static bool HasNoBuildNumber(this Version value) => value?.Build == -1;
    [GenerateAssertion(ExpectationMessage = "to have a revision number", InlineMethodBody = true)]
    public static bool HasRevisionNumber(this Version value) => value?.Revision >= 0;
    [GenerateAssertion(ExpectationMessage = "to not have a revision number", InlineMethodBody = true)]
    public static bool HasNoRevisionNumber(this Version value) => value?.Revision == -1;
}
