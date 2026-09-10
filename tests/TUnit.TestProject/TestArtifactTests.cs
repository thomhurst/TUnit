using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TestArtifactTests
{
    [Test]
    public void Artifact_Test()
    {
        TestContext.Current!.Output.AttachArtifact(new Artifact
        {
            File = new FileInfo("Data/Zip.zip"),
            DisplayName = "Blah!"
        });
    }

    [Test]
    public void Artifact_Test_Simple_Overload()
    {
        // Simple overload - file name is used as display name
        TestContext.Current!.Output.AttachArtifact("Data/Zip.zip");
    }

    [Test]
    public void Artifact_Test_Simple_Overload_With_Name()
    {
        // Simple overload with custom display name and description
        TestContext.Current!.Output.AttachArtifact(
            "Data/Zip.zip",
            displayName: "Test Zip File",
            description: "A sample zip file for testing"
        );
    }
}
