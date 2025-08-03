using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// This test will help verify extern alias functionality
// Even without actual aliases, we want to ensure the generated code compiles correctly
[EngineTest(ExpectedResult.Pass)]
public class ExternAliasTests
{
    [ClassDataSource<FileSystemMock>(Shared = SharedType.PerTestSession)]
    public required FileSystemMock FileSystem { get; init; }

    [Test]
    public async Task TestWithMockFileSystem()
    {
        await Assert.That(FileSystem).IsNotNull();
    }
}

// A mock class to simulate what would be in an extern aliased assembly
public class FileSystemMock
{
    public bool IsInitialized { get; set; } = true;
}