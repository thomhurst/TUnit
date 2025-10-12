using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for FileInfo type using [AssertionFrom&lt;FileInfo&gt;] and [GenerateAssertion] attributes.
/// These wrap file property checks as extension methods.
/// </summary>
[AssertionFrom<FileInfo>(nameof(FileInfo.Exists), ExpectationMessage = "exist")]
[AssertionFrom<FileInfo>(nameof(FileInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true, ExpectationMessage = "exist")]

[AssertionFrom<FileInfo>(nameof(FileInfo.IsReadOnly), ExpectationMessage = "be read-only")]
[AssertionFrom<FileInfo>(nameof(FileInfo.IsReadOnly), CustomName = "IsNotReadOnly", NegateLogic = true, ExpectationMessage = "be read-only")]
public static partial class FileInfoAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have an extension")]
    public static bool HasExtension(this FileInfo value) => !string.IsNullOrEmpty(value?.Extension);

    [GenerateAssertion(ExpectationMessage = "to not have an extension")]
    public static bool HasNoExtension(this FileInfo value) => string.IsNullOrEmpty(value?.Extension);

    [GenerateAssertion(ExpectationMessage = "to be hidden")]
    public static bool IsHidden(this FileInfo value) => value?.Attributes.HasFlag(FileAttributes.Hidden) == true;

    [GenerateAssertion(ExpectationMessage = "to not be hidden")]
    public static bool IsNotHidden(this FileInfo value) => value?.Attributes.HasFlag(FileAttributes.Hidden) == false;

    [GenerateAssertion(ExpectationMessage = "to be empty")]
    public static bool IsEmpty(this FileInfo value) => value?.Length == 0;

    [GenerateAssertion(ExpectationMessage = "to not be empty")]
    public static bool IsNotEmpty(this FileInfo value) => value != null && value.Length > 0;

    [GenerateAssertion(ExpectationMessage = "to be a system file")]
    public static bool IsSystemFile(this FileInfo value) => value?.Attributes.HasFlag(FileAttributes.System) == true;

    [GenerateAssertion(ExpectationMessage = "to be archived")]
    public static bool IsArchived(this FileInfo value) => value?.Attributes.HasFlag(FileAttributes.Archive) == true;
}
