using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// FileInfo assertions
[CreateAssertion(typeof(FileInfo), nameof(FileInfo.Exists))]
[CreateAssertion(typeof(FileInfo), nameof(FileInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true)]

[CreateAssertion(typeof(FileInfo), nameof(FileInfo.IsReadOnly))]
[CreateAssertion(typeof(FileInfo), nameof(FileInfo.IsReadOnly), CustomName = "IsNotReadOnly", NegateLogic = true)]

// DirectoryInfo assertions
[CreateAssertion(typeof(DirectoryInfo), nameof(DirectoryInfo.Exists))]
[CreateAssertion(typeof(DirectoryInfo), nameof(DirectoryInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true)]

// FileSystemInfo common assertions (base class for both FileInfo and DirectoryInfo)
[CreateAssertion(typeof(FileSystemInfo), nameof(FileSystemInfo.Exists))]
[CreateAssertion(typeof(FileSystemInfo), nameof(FileSystemInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true)]
public static partial class FileSystemAssertionExtensions;
