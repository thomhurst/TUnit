using System.IO;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class FileSystemAssertionTests
{
    private string _testDirectory = null!;
    private string _testFile = null!;

    [Before(Test)]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _testFile = Path.Combine(_testDirectory, "test.txt");
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_testFile, "test content");
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public async Task Test_DirectoryInfo_Exists()
    {
        var directory = new DirectoryInfo(_testDirectory);
        await Assert.That(directory).Exists();
    }

    [Test]
    public async Task Test_DirectoryInfo_DoesNotExist()
    {
        var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        await Assert.That(directory).DoesNotExist();
    }

    [Test]
    public async Task Test_DirectoryInfo_IsNotEmpty()
    {
        var directory = new DirectoryInfo(_testDirectory);
        await Assert.That(directory).IsNotEmpty();
    }

    [Test]
    public async Task Test_DirectoryInfo_HasFiles()
    {
        var directory = new DirectoryInfo(_testDirectory);
        await Assert.That(directory).HasFiles();
    }

    [Test]
    public async Task Test_DirectoryInfo_HasNoSubdirectories()
    {
        var directory = new DirectoryInfo(_testDirectory);
        await Assert.That(directory).HasNoSubdirectories();
    }

    [Test]
    public async Task Test_FileInfo_Exists()
    {
        var file = new FileInfo(_testFile);
        await Assert.That(file).Exists();
    }

    [Test]
    public async Task Test_FileInfo_DoesNotExist()
    {
        var file = new FileInfo(Path.Combine(_testDirectory, "nonexistent.txt"));
        await Assert.That(file).DoesNotExist();
    }

    [Test]
    public async Task Test_FileInfo_IsNotEmpty()
    {
        var file = new FileInfo(_testFile);
        await Assert.That(file).IsNotEmpty();
    }

    [Test]
    public async Task Test_FileInfo_IsNotReadOnly()
    {
        var file = new FileInfo(_testFile);
        await Assert.That(file).IsNotReadOnly();
    }

    [Test]
    public async Task Test_FileInfo_IsNotHidden()
    {
        var file = new FileInfo(_testFile);
        await Assert.That(file).IsNotHidden();
    }

    [Test]
    public async Task Test_FileInfo_IsNotSystem()
    {
        var file = new FileInfo(_testFile);
        await Assert.That(file).IsNotSystem();
    }

    [Test]
    public async Task Test_FileInfo_IsNotExecutable()
    {
        var file = new FileInfo(_testFile);
        await Assert.That(file).IsNotExecutable();
    }
}