using System.ComponentModel;
using System.IO;
using System.Linq;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DirectoryInfo type using [AssertionFrom&lt;DirectoryInfo&gt;] and [GenerateAssertion] attributes.
/// These wrap directory property checks as extension methods.
/// </summary>
[AssertionFrom<DirectoryInfo>(nameof(DirectoryInfo.Exists), ExpectationMessage = "exist")]
[AssertionFrom<DirectoryInfo>(nameof(DirectoryInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true, ExpectationMessage = "exist")]
public static partial class DirectoryInfoAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be empty")]
    public static bool IsEmpty(this DirectoryInfo value) => value != null && !value.EnumerateFileSystemInfos().Any();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be empty")]
    public static bool IsNotEmpty(this DirectoryInfo value) => value != null && value.EnumerateFileSystemInfos().Any();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a root directory")]
    public static bool IsRoot(this DirectoryInfo value) => value?.Parent == null;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be a root directory")]
    public static bool IsNotRoot(this DirectoryInfo value) => value?.Parent != null;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be hidden")]
    public static bool IsHidden(this DirectoryInfo value) => value?.Attributes.HasFlag(FileAttributes.Hidden) == true;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be hidden")]
    public static bool IsNotHidden(this DirectoryInfo value) => value?.Attributes.HasFlag(FileAttributes.Hidden) == false;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a system directory")]
    public static bool IsSystemDirectory(this DirectoryInfo value) => value?.Attributes.HasFlag(FileAttributes.System) == true;
}
