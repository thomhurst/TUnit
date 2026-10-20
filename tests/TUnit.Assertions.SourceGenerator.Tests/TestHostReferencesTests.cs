using TUnit.Tests.Shared;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Pins how <see cref="TestHostReferences.CreateReferences(System.Reflection.Assembly[])"/> builds the metadata
/// references every generator test compilation uses.
/// </summary>
internal class TestHostReferencesTests
{
    [Test]
    public async Task CreateReferences_Lists_The_Host_Assemblies_Then_The_Additional_Ones()
    {
        var additional = typeof(TestHostReferencesTests).Assembly;

        var paths = TestHostReferences.CreateReferences(additional).Select(reference => reference.FilePath).ToArray();

        await Assert.That(paths.Take(TestHostReferences.AssemblyPaths.Count)).IsEquivalentTo(TestHostReferences.AssemblyPaths, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(paths.Skip(TestHostReferences.AssemblyPaths.Count)).IsEquivalentTo(new[] { additional.Location }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task CreateReferences_Leaves_Out_The_Host_Assemblies_The_Filter_Excludes()
    {
        var excluded = TestHostReferences.AssemblyPaths[0];

        var paths = TestHostReferences.CreateReferences(path => path != excluded).Select(reference => reference.FilePath).ToArray();

        await Assert.That(paths).DoesNotContain(excluded);
        await Assert.That(paths.Length).IsEqualTo(TestHostReferences.AssemblyPaths.Count - 1);
    }
}
