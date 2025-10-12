using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Version type using [GenerateAssertion] attributes.
/// These wrap version number checks as extension methods.
/// </summary>
public static partial class VersionAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a major version (x.0.0.0)")]
    public static bool IsMajorVersion(this Version value) =>
        value != null && value.Minor == 0 && (value.Build <= 0 || value.Build == -1) && (value.Revision <= 0 || value.Revision == -1);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be a major version")]
    public static bool IsNotMajorVersion(this Version value) =>
        value != null && (value.Minor != 0 || (value.Build > 0 && value.Build != -1) || (value.Revision > 0 && value.Revision != -1));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have a build number")]
    public static bool HasBuildNumber(this Version value) => value?.Build >= 0;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not have a build number")]
    public static bool HasNoBuildNumber(this Version value) => value?.Build == -1;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have a revision number")]
    public static bool HasRevisionNumber(this Version value) => value?.Revision >= 0;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not have a revision number")]
    public static bool HasNoRevisionNumber(this Version value) => value?.Revision == -1;
}
