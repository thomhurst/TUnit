using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<DirectoryInfo>( nameof(DirectoryInfo.Exists))]
[CreateAssertion<DirectoryInfo>( nameof(DirectoryInfo.Exists), CustomName = "DoesNotExist", NegateLogic = true)]

// Custom helper methods
[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(IsEmpty))]
[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true)]

[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(IsHidden))]
[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(IsHidden), CustomName = "IsNotHidden", NegateLogic = true)]

[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(IsReadOnly))]
[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(IsReadOnly), CustomName = "IsNotReadOnly", NegateLogic = true)]

[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(IsSystem))]
[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(IsSystem), CustomName = "IsNotSystem", NegateLogic = true)]

[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(HasSubdirectories))]
[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(HasSubdirectories), CustomName = "HasNoSubdirectories", NegateLogic = true)]

[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(HasFiles))]
[CreateAssertion<DirectoryInfo>( typeof(DirectoryInfoAssertionExtensions), nameof(HasFiles), CustomName = "HasNoFiles", NegateLogic = true)]
public static partial class DirectoryInfoAssertionExtensions
{
    internal static bool IsEmpty(DirectoryInfo directory) => 
        directory.Exists && directory.GetFileSystemInfos().Length == 0;
    
    internal static bool IsHidden(DirectoryInfo directory) => 
        (directory.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
    
    internal static bool IsReadOnly(DirectoryInfo directory) => 
        (directory.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
    
    internal static bool IsSystem(DirectoryInfo directory) => 
        (directory.Attributes & FileAttributes.System) == FileAttributes.System;
    
    internal static bool HasSubdirectories(DirectoryInfo directory) => 
        directory.Exists && directory.GetDirectories().Length > 0;
    
    internal static bool HasFiles(DirectoryInfo directory) => 
        directory.Exists && directory.GetFiles().Length > 0;
}