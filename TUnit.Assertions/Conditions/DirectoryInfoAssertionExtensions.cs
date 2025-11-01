using System.IO;
using System.Linq;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DirectoryInfo type using [AssertionFrom&lt;DirectoryInfo&gt;] and [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap directory property checks as extension methods.
/// </summary>
[AssertionFrom<DirectoryInfo>(nameof(DirectoryInfo.Exists), ExpectationMessage = "exist")]
[AssertionFrom<DirectoryInfo>(nameof(DirectoryInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true, ExpectationMessage = "exist")]
file static partial class DirectoryInfoAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be empty", InlineMethodBody = true)]
    public static bool IsEmpty(this DirectoryInfo value) => value != null && !value.EnumerateFileSystemInfos().Any();
    [GenerateAssertion(ExpectationMessage = "to not be empty", InlineMethodBody = true)]
    public static bool IsNotEmpty(this DirectoryInfo value) => value != null && value.EnumerateFileSystemInfos().Any();
    [GenerateAssertion(ExpectationMessage = "to be a root directory", InlineMethodBody = true)]
    public static bool IsRoot(this DirectoryInfo value) => value?.Parent == null;
    [GenerateAssertion(ExpectationMessage = "to not be a root directory", InlineMethodBody = true)]
    public static bool IsNotRoot(this DirectoryInfo value) => value?.Parent != null;
    [GenerateAssertion(ExpectationMessage = "to be hidden", InlineMethodBody = true)]
    public static bool IsHidden(this DirectoryInfo value) => value?.Attributes.HasFlag(FileAttributes.Hidden) == true;
    [GenerateAssertion(ExpectationMessage = "to not be hidden", InlineMethodBody = true)]
    public static bool IsNotHidden(this DirectoryInfo value) => value?.Attributes.HasFlag(FileAttributes.Hidden) == false;
    [GenerateAssertion(ExpectationMessage = "to be a system directory", InlineMethodBody = true)]
    public static bool IsSystemDirectory(this DirectoryInfo value) => value?.Attributes.HasFlag(FileAttributes.System) == true;
}
