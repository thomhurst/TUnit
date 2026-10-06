using TUnit.Assertions.Extensions;
using TUnit.Core.Enums;
using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

public class PathValidatorTests
{
    [Test]
    public async Task SanitizeFileName_CleanName_ReturnsSameReference()
    {
        var name = "MyAssembly.Tests";

        var result = PathValidator.SanitizeFileName(name);

        // Fast path: no invalid chars, the original instance is returned unchanged.
        await Assert.That(result).IsSameReferenceAs(name);
    }

    [Test]
    public async Task SanitizeFileName_EmptyString_ReturnsSameReference()
    {
        var name = string.Empty;

        var result = PathValidator.SanitizeFileName(name);

        await Assert.That(result).IsSameReferenceAs(name);
    }

    [Test]
    public async Task SanitizeFileName_StripsPathSeparators()
    {
        // '/' is invalid on every platform; '\' is invalid on Windows.
        var result = PathValidator.SanitizeFileName("foo/bar");

        await Assert.That(result).DoesNotContain("/");
    }

    [Test]
    public async Task SanitizeFileName_StripsInvalidChars()
    {
        var invalid = Path.GetInvalidFileNameChars();

        // Skip platforms with no invalid chars (none in practice, but keep the test honest).
        if (invalid.Length == 0)
        {
            return;
        }

        var name = $"a{invalid[0]}b";

        var result = PathValidator.SanitizeFileName(name);

        await Assert.That(result).IsEqualTo("ab");
    }

    [Test]
    public async Task SanitizeFileName_LongNameWithInvalidChar_ExercisesHeapBranch()
    {
        var invalid = Path.GetInvalidFileNameChars();

        if (invalid.Length == 0)
        {
            return;
        }

        // > 256 chars forces the heap-allocated (non-stackalloc) slow path.
        var prefix = new string('a', 300);
        var name = prefix + invalid[0];

        var result = PathValidator.SanitizeFileName(name);

        await Assert.That(result).IsEqualTo(prefix);
    }

    [Test]
    public async Task IsWithinDirectory_Child_IsInside()
    {
        var root = Path.GetFullPath(Path.GetTempPath());
        var child = Path.Combine(root, "reports", "out.html");

        await Assert.That(PathValidator.IsWithinDirectory(child, root)).IsTrue();
    }

    [Test]
    public async Task IsWithinDirectory_DirectoryItself_IsInside()
    {
        var root = Path.GetFullPath(Path.GetTempPath());

        await Assert.That(PathValidator.IsWithinDirectory(root, root)).IsTrue();
    }

    [Test]
    public async Task IsWithinDirectory_SiblingSharingPrefix_IsOutside()
    {
        var parent = Path.GetFullPath(Path.GetTempPath());
        var directory = Path.Combine(parent, "bar");
        var sibling = Path.Combine(parent, "barbaz", "file.txt");

        await Assert.That(PathValidator.IsWithinDirectory(sibling, directory)).IsFalse();
    }

    [Test]
    [ExcludeOn(OS.Windows)]
    public async Task IsWithinDirectory_DifferentlyCasedSibling_IsOutside_On_CaseSensitive_Platforms()
    {
        await Assert.That(PathValidator.IsWithinDirectory("/work/Project/file.txt", "/work/project")).IsFalse();
    }

    [Test]
    [RunOn(OS.Windows)]
    public async Task IsWithinDirectory_FoldsCase_On_Windows()
    {
        await Assert.That(PathValidator.IsWithinDirectory(@"C:\Work\Project\file.txt", @"c:\work\project")).IsTrue();
    }

    [Test]
    public async Task IsWithinDirectory_FileSystemRoot_ContainsEverything()
    {
        var root = Path.GetPathRoot(Path.GetFullPath(Path.GetTempPath()))!;

        await Assert.That(PathValidator.IsWithinDirectory(Path.GetFullPath(Path.GetTempPath()), root)).IsTrue();
    }
}
