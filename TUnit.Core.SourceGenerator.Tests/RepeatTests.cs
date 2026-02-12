
namespace TUnit.Core.SourceGenerator.Tests;

internal class RepeatTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "RepeatTests.cs"),
        async generatedFiles =>
        {
            });

    [Test]
    public async Task Assembly_Level_Repeat()
    {
        var source = """
            using TUnit.Core;

            [assembly: Repeat(3)]

            namespace TUnit.TestProject;

            public class AssemblyRepeatTests
            {
                [Test]
                public void TestWithAssemblyRepeat()
                {
                }

                [Test]
                [Repeat(1)]
                public void TestWithMethodRepeatOverride()
                {
                }
            }
            """;

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        try
        {
            await TestMetadataGenerator.RunTest(tempFile, async generatedFiles =>
            {
            });
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
