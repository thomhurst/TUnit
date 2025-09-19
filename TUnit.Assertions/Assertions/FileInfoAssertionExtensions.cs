using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<FileInfo>( nameof(FileInfo.Exists))]
[CreateAssertion<FileInfo>( nameof(FileInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true)]

[CreateAssertion<FileInfo>( nameof(FileInfo.IsReadOnly))]
[CreateAssertion<FileInfo>( nameof(FileInfo.IsReadOnly), CustomName = "IsNotReadOnly", NegateLogic = true)]

// Custom helper methods
[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsEmpty))]
[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true)]

[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsHidden))]
[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsHidden), CustomName = "IsNotHidden", NegateLogic = true)]

[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsSystem))]
[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsSystem), CustomName = "IsNotSystem", NegateLogic = true)]

[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsExecutable))]
[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsExecutable), CustomName = "IsNotExecutable", NegateLogic = true)]

[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsCompressed))]
[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsCompressed), CustomName = "IsNotCompressed", NegateLogic = true)]

[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsEncrypted))]
[CreateAssertion<FileInfo>( typeof(FileInfoAssertionExtensions), nameof(IsEncrypted), CustomName = "IsNotEncrypted", NegateLogic = true)]
public static partial class FileInfoAssertionExtensions
{
    internal static bool IsEmpty(FileInfo file) => 
        file.Exists && file.Length == 0;
    
    internal static bool IsHidden(FileInfo file) => 
        (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
    
    internal static bool IsSystem(FileInfo file) => 
        (file.Attributes & FileAttributes.System) == FileAttributes.System;
    
    internal static bool IsExecutable(FileInfo file) => 
        file.Extension.Equals(".exe", System.StringComparison.OrdinalIgnoreCase) ||
        file.Extension.Equals(".dll", System.StringComparison.OrdinalIgnoreCase) ||
        file.Extension.Equals(".com", System.StringComparison.OrdinalIgnoreCase) ||
        file.Extension.Equals(".bat", System.StringComparison.OrdinalIgnoreCase) ||
        file.Extension.Equals(".cmd", System.StringComparison.OrdinalIgnoreCase) ||
        file.Extension.Equals(".sh", System.StringComparison.OrdinalIgnoreCase);
    
    internal static bool IsCompressed(FileInfo file) => 
        (file.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed;
    
    internal static bool IsEncrypted(FileInfo file) => 
        (file.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted;
}