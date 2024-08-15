namespace TUnit.TestProject;

public class TestArtifactTests
{
    [Test]
    public void Artifact_Test()
    {
        TestContext.Current!.AddArtifact(new Artifact
        {
            File = new FileInfo("Data/Zip.zip"),
            DisplayName = "Blah!"
        });
    }
}