using TUnit.Engine.Discovery;

namespace TUnit.UnitTests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7092: reflection discovery excluded every assembly whose
/// path merely contained "ref" or "runtimes", so a checkout under e.g. "reflection-tests" found no tests.
/// </summary>
public class ReflectionAssemblyLocationFilterTests
{
    [Test]
    [Arguments("ref")]
    [Arguments("runtimes")]
    [Arguments("Microsoft.NETCore.App")]
    public async Task Framework_And_Reference_Directories_Are_Excluded(string directory)
    {
        var location = Path.GetFullPath(Path.Combine("/", "dotnet", "packs", directory, "net10.0", "Some.dll"));

        await Assert.That(ReflectionTestDataCollector.IsFrameworkOrReferenceAssemblyLocation(location)).IsTrue();
    }

    [Test]
    [Arguments("reflection-unittests")]
    [Arguments("refactoring")]
    [Arguments("preferences")]
    [Arguments("my-runtimes-app")]
    public async Task Directories_That_Only_Contain_The_Words_Are_Not_Excluded(string directory)
    {
        var location = Path.GetFullPath(Path.Combine("/", "src", directory, "bin", "Debug", "net10.0", "MyTests.dll"));

        await Assert.That(ReflectionTestDataCollector.IsFrameworkOrReferenceAssemblyLocation(location)).IsFalse();
    }
}
