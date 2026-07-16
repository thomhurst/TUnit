using TUnit.Assertions.Extensions;
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
}
