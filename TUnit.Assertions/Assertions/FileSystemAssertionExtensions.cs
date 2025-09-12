using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// FileInfo-specific assertions are in FileInfoAssertionExtensions.cs
// DirectoryInfo-specific assertions are in DirectoryInfoAssertionExtensions.cs

// FileSystemInfo common assertions (base class for both FileInfo and DirectoryInfo)
[CreateAssertion(typeof(FileSystemInfo), nameof(FileSystemInfo.Exists))]
[CreateAssertion(typeof(FileSystemInfo), nameof(FileSystemInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true)]
public static partial class FileSystemAssertionExtensions;
