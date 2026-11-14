namespace TUnit.Core.SourceGenerator.Tests;

internal class DependsOnNameEscapingTests : TestsBase
{
    [Test]
    public async Task Names_With_Quotes_And_Backslashes_Are_Escaped()
    {
        var source = """
            using TUnit.Core;

            namespace TUnit.TestProject;

            public class DependsOnEscapingTarget
            {
                [Test]
                public void Target()
                {
                }
            }

            public class DependsOnEscapingTests
            {
                [Test]
                [DependsOn("Same\"Class\\")]
                [DependsOn("Params\"\\", [typeof(int)])]
                [DependsOn<DependsOnEscapingTarget>("Generic\"\\")]
                [DependsOn<DependsOnEscapingTarget>("GenericParams\"\\", [typeof(int)])]
                [DependsOn(typeof(DependsOnEscapingTarget), "Typed\"\\")]
                [DependsOn(typeof(DependsOnEscapingTarget), "TypedParams\"\\", [typeof(int)])]
                public void Dependent()
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
                var generated = string.Join(Environment.NewLine, generatedFiles);
                await Assert.That(generated).Contains("MethodName = \"Same\\\"Class\\\\\"");
                await Assert.That(generated).Contains("MethodName = \"Params\\\"\\\\\"");
                await Assert.That(generated).Contains("MethodName = \"Generic\\\"\\\\\"");
                await Assert.That(generated).Contains("MethodName = \"GenericParams\\\"\\\\\"");
                await Assert.That(generated).Contains("MethodName = \"Typed\\\"\\\\\"");
                await Assert.That(generated).Contains("MethodName = \"TypedParams\\\"\\\\\"");
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
